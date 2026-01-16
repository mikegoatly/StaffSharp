"""
Training script for Onsets and Frames polyphonic piano transcription model.

Usage:
    # Train on prepared MAESTRO dataset
    python train.py --data-dir //studyfiles/home/maestro-v3.0.0-processed --epochs 100

    # Resume from checkpoint
    python train.py --data-dir data/processed --resume models/checkpoint_epoch_50.pt

    # Train on CPU (if GPU incompatible)
    python train.py --data-dir data/processed --device cpu
"""

import argparse
import os
import random
import time
from pathlib import Path
from typing import Dict, Optional

import numpy as np
import torch
import torch.nn as nn
from torch.utils.data import Dataset, DataLoader
from torch.utils.tensorboard import SummaryWriter
from tqdm import tqdm

from model import OnsetsAndFramesModel, OnsetsAndFramesLoss, create_model
from early_stopping import EarlyStopping
from metrics import compute_all_metrics, print_metrics


def set_seed(seed: int = 42):
    """Set random seeds for reproducibility across all libraries.
    
    Args:
        seed: Random seed value (default: 42)
    """
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    
    if torch.cuda.is_available():
        torch.cuda.manual_seed_all(seed)
        # For full determinism (may reduce performance slightly)
        torch.backends.cudnn.deterministic = True
        torch.backends.cudnn.benchmark = False


class MaestroDataset(Dataset):
    """Dataset for preprocessed MAESTRO data."""

    def __init__(self, data_dir: str, split: str = 'train', sequence_length: Optional[int] = None):
        """
        Initialize dataset.

        Args:
            data_dir: Directory containing processed .npz files
            split: 'train', 'validation', or 'test'
            sequence_length: Maximum sequence length in frames. Longer sequences are cropped.
        """
        self.data_dir = Path(data_dir) / split
        self.split = split
        self.sequence_length = sequence_length
        self.files = sorted(list(self.data_dir.glob('*.npz')))

        if len(self.files) == 0:
            raise ValueError(f"No .npz files found in {self.data_dir}")

        print(f"Loaded {len(self.files)} files for {split} split")

    def __len__(self):
        return len(self.files)

    def __getitem__(self, idx):
        # Load preprocessed data with error handling
        try:
            data = np.load(self.files[idx])
            
            # Get raw arrays - this is where corruption errors often occur
            m_spec = data['mel_spec']
            o_roll = data['onset_roll']
            off_roll = data['offset_roll']
            p_roll = data['piano_roll']
            v_roll = data['velocity_roll']
            
        except KeyError as e:
            print(f"\n⚠️  Missing key {e} in {self.files[idx]}")
            print(f"    File may be corrupted or from an old preprocessing version. Skipping...")
            next_idx = (idx + 1) % len(self.files)
            if next_idx == idx:
                raise RuntimeError(f"Missing required keys in file and no other files available: {self.files[idx]}")
            return self.__getitem__(next_idx)
            
        except Exception as e:
            # Catch all other errors: BadZipFile, corrupted files, read errors, etc.
            print(f"\n⚠️  CORRUPTED FILE DETECTED: {self.files[idx].name}")
            print(f"    Error: {type(e).__name__}: {e}")
            print(f"    This file should be deleted and re-processed. Skipping for now...")
            
            # Skip to next file instead of crashing
            next_idx = (idx + 1) % len(self.files)
            if next_idx == idx:  # Prevent infinite loop if only one file
                raise RuntimeError(f"Failed to load file and no other files available: {self.files[idx]}")
            return self.__getitem__(next_idx)

        # Crop if necessary
        if self.sequence_length and m_spec.shape[0] > self.sequence_length:
            if self.split == 'train':
                start = np.random.randint(0, m_spec.shape[0] - self.sequence_length)
            else:
                # Deterministic crop (middle) for validation/test to ensure consistency
                start = (m_spec.shape[0] - self.sequence_length) // 2

            end = start + self.sequence_length
            m_spec = m_spec[start:end]
            o_roll = o_roll[start:end]
            off_roll = off_roll[start:end]
            p_roll = p_roll[start:end]
            v_roll = v_roll[start:end]

        mel_spec = torch.from_numpy(m_spec)
        onset_roll = torch.from_numpy(o_roll)
        offset_roll = torch.from_numpy(off_roll)  # Use pre-computed offset_roll from C#
        piano_roll = torch.from_numpy(p_roll)
        velocity_roll = torch.from_numpy(v_roll)

        return mel_spec, onset_roll, offset_roll, piano_roll, velocity_roll


class SlidingWindowDataset(Dataset):
    """
    Wraps MaestroDataset to yield multiple sliding window crops per file.
    
    This increases effective dataset size and training efficiency by using
    overlapping windows from each file instead of random single crops per epoch.
    
    Example:
        For a 30-second file (18,000 frames) with 2000-frame crops and 500-frame stride:
        - Single crop: 1 sample per epoch
        - Sliding window: 36 samples per epoch (18000 - 2000) / 500 + 1
        - Data utilization: 89% -> ~99%
    """
    
    def __init__(
        self,
        base_dataset: MaestroDataset,
        stride: int = 500,
        use_all_windows: bool = True
    ):
        """
        Initialize sliding window wrapper.
        
        Args:
            base_dataset: MaestroDataset instance
            stride: Number of frames to slide between windows (default: 500)
            use_all_windows: If True, use all possible windows. If False, use single random crop per file
                            (for memory efficiency on large datasets)
        """
        self.base_dataset = base_dataset
        self.stride = stride
        self.use_all_windows = use_all_windows
        self.sequence_length = base_dataset.sequence_length
        
        # Build index mapping file indices to window indices
        self.windows = []  # List of (file_idx, window_start_frame)
        
        if self.use_all_windows and self.sequence_length:
            # Pre-compute all windows
            for file_idx in range(len(base_dataset)):
                try:
                    data = np.load(base_dataset.files[file_idx])
                    num_frames = data['mel_spec'].shape[0]
                    
                    if num_frames >= self.sequence_length:
                        # Generate all windows with stride
                        for start in range(0, num_frames - self.sequence_length + 1, self.stride):
                            self.windows.append((file_idx, start))
                    else:
                        # File too short, use as-is
                        self.windows.append((file_idx, 0))
                except Exception as e:
                    print(f"Warning: could not load {base_dataset.files[file_idx]}: {e}")
                    continue
            
            print(f"Created {len(self.windows)} sliding window samples from {len(base_dataset)} files")
            if len(self.windows) == 0:
                raise ValueError("No valid windows could be created from dataset")
        else:
            # Fall back to single sample per file
            self.windows = [(i, 0) for i in range(len(base_dataset))]
    
    def __len__(self):
        return len(self.windows)
    
    def __getitem__(self, idx):
        file_idx, start_frame = self.windows[idx]
        
        # Load the file directly
        try:
            data = np.load(self.base_dataset.files[file_idx])
        except Exception as e:
            print(f"Error loading {self.base_dataset.files[file_idx]}: {e}")
            # Skip to next window
            next_idx = (idx + 1) % len(self.windows)
            if next_idx == idx:
                raise RuntimeError("Cannot load any files in dataset")
            return self.__getitem__(next_idx)
        
        try:
            m_spec = data['mel_spec']
            o_roll = data['onset_roll']
            off_roll = data['offset_roll']
            p_roll = data['piano_roll']
            v_roll = data['velocity_roll']
        except KeyError as e:
            print(f"Missing key {e} in {self.base_dataset.files[file_idx]}")
            next_idx = (idx + 1) % len(self.windows)
            if next_idx == idx:
                raise RuntimeError("Cannot load data from any files in dataset")
            return self.__getitem__(next_idx)

        # Extract window
        if self.sequence_length:
            end_frame = start_frame + self.sequence_length
            m_spec = m_spec[start_frame:end_frame]
            o_roll = o_roll[start_frame:end_frame]
            off_roll = off_roll[start_frame:end_frame]
            p_roll = p_roll[start_frame:end_frame]
            v_roll = v_roll[start_frame:end_frame]

        mel_spec = torch.from_numpy(m_spec)
        onset_roll = torch.from_numpy(o_roll)
        offset_roll = torch.from_numpy(off_roll)  # Use pre-computed offset_roll from C#
        piano_roll = torch.from_numpy(p_roll)
        velocity_roll = torch.from_numpy(v_roll)

        return mel_spec, onset_roll, offset_roll, piano_roll, velocity_roll


def collate_fn(batch):
    """
    Collate function to handle variable-length sequences in batches.
    
    Pads all sequences to the longest in the batch and creates a binary mask
    to indicate real data vs padding. This is essential for correctly computing
    loss on variable-length sequences without penalizing padding frames.
    
    Args:
        batch: List of tuples from MaestroDataset:
            (mel_spec, onset_roll, offset_roll, piano_roll, velocity_roll)
            Each with shape (T_i, feature_dim) where T_i can vary per sample
    
    Returns:
        Tuple of:
            - mel_specs: (B, max_T, 229) - Padded mel spectrograms
            - onset_rolls: (B, max_T, 88) - Padded onset labels
            - offset_rolls: (B, max_T, 88) - Padded offset labels
            - piano_rolls: (B, max_T, 88) - Padded frame labels
            - velocity_rolls: (B, max_T, 88) - Padded velocity labels
            - mask: (B, max_T) - Binary mask (1.0=real data, 0.0=padding)
    
    Note:
        The mask is used in the loss function to ignore padded frames:
        - Loss is computed element-wise, then masked and averaged
        - This prevents the model from learning to predict zeros for padding
        - Ensures fair comparison between sequences of different lengths
    """
    mel_specs, onset_rolls, offset_rolls, piano_rolls, velocity_rolls = zip(*batch)

    # Get actual lengths for masking
    lengths = [m.shape[0] for m in mel_specs]
    max_len = max(lengths)

    # Pad all sequences
    def pad_sequence(seq, max_len):
        if seq.shape[0] < max_len:
            padding = torch.zeros(max_len - seq.shape[0], *seq.shape[1:])
            return torch.cat([seq, padding], dim=0)
        return seq

    mel_specs = torch.stack([pad_sequence(m, max_len) for m in mel_specs])
    onset_rolls = torch.stack([pad_sequence(o, max_len) for o in onset_rolls])
    offset_rolls = torch.stack([pad_sequence(o, max_len) for o in offset_rolls])
    piano_rolls = torch.stack([pad_sequence(p, max_len) for p in piano_rolls])
    velocity_rolls = torch.stack([pad_sequence(v, max_len) for v in velocity_rolls])

    # Create mask: (B, T) - 1 for real data, 0 for padding
    mask = torch.zeros(len(batch), max_len)
    for i, length in enumerate(lengths):
        mask[i, :length] = 1.0

    return mel_specs, onset_rolls, offset_rolls, piano_rolls, velocity_rolls, mask


def train_epoch(
    model: nn.Module,
    dataloader: DataLoader,
    criterion: nn.Module,
    optimizer: torch.optim.Optimizer,
    device: str,
    epoch: int,
    writer: Optional[SummaryWriter] = None,
    scaler: Optional[torch.cuda.amp.GradScaler] = None
) -> Dict[str, float]:
    """Train for one epoch."""
    model.train()

    total_loss = 0
    total_onset_loss = 0
    total_offset_loss = 0
    total_frame_loss = 0
    total_velocity_loss = 0
    num_batches = 0

    pbar = tqdm(dataloader, desc=f"Epoch {epoch}")

    for batch_idx, (mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask) in enumerate(pbar):
        # Move to device
        mel_specs = mel_specs.to(device)
        onset_targets = onset_targets.to(device)
        offset_targets = offset_targets.to(device)
        frame_targets = frame_targets.to(device)
        velocity_targets = velocity_targets.to(device)
        mask = mask.to(device)

        # Forward pass
        with torch.cuda.amp.autocast(enabled=(scaler is not None)):
            onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)

            # Compute loss with mask to ignore padding
            loss, loss_dict = criterion(
                onset_pred, offset_pred, frame_pred, velocity_pred,
                onset_targets, offset_targets, frame_targets, velocity_targets,
                mask=mask
            )

        # Backward pass
        optimizer.zero_grad()
        
        if scaler:
            scaler.scale(loss).backward()
            scaler.unscale_(optimizer)
            torch.nn.utils.clip_grad_norm_(model.parameters(), max_norm=1.0)
            scaler.step(optimizer)
            scaler.update()
        else:
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), max_norm=1.0)
            optimizer.step()

        # Accumulate losses
        total_loss += loss_dict['total']
        total_onset_loss += loss_dict['onset']
        total_offset_loss += loss_dict['offset']
        total_frame_loss += loss_dict['frame']
        total_velocity_loss += loss_dict['velocity']
        num_batches += 1

        # Update progress bar
        pbar.set_postfix({
            'loss': f"{loss_dict['total']:.4f}",
            'onset': f"{loss_dict['onset']:.4f}",
            'offset': f"{loss_dict['offset']:.4f}",
            'frame': f"{loss_dict['frame']:.4f}",
            'vel': f"{loss_dict['velocity']:.4f}"
        })

        # TensorBoard logging
        if writer and batch_idx % 10 == 0:
            global_step = epoch * len(dataloader) + batch_idx
            writer.add_scalar('train/batch_loss', loss_dict['total'], global_step)
            writer.add_scalar('train/batch_onset_loss', loss_dict['onset'], global_step)
            writer.add_scalar('train/batch_offset_loss', loss_dict['offset'], global_step)
            writer.add_scalar('train/batch_frame_loss', loss_dict['frame'], global_step)
            writer.add_scalar('train/batch_velocity_loss', loss_dict['velocity'], global_step)

    # Compute epoch averages
    metrics = {
        'loss': total_loss / num_batches,
        'onset_loss': total_onset_loss / num_batches,
        'offset_loss': total_offset_loss / num_batches,
        'frame_loss': total_frame_loss / num_batches,
        'velocity_loss': total_velocity_loss / num_batches
    }

    return metrics


def validate(
    model: nn.Module,
    dataloader: DataLoader,
    criterion: nn.Module,
    device: str
) -> Dict[str, float]:
    """Validate the model and compute metrics."""
    model.eval()

    total_loss = 0
    total_onset_loss = 0
    total_offset_loss = 0
    total_frame_loss = 0
    total_velocity_loss = 0
    num_batches = 0
    
    # For computing metrics
    all_onset_probs = []
    all_offset_probs = []
    all_frame_probs = []
    all_velocity_preds = []
    all_onset_labels = []
    all_offset_labels = []
    all_frame_labels = []
    all_velocity_labels = []
    all_masks = []

    with torch.no_grad():
        for mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask in tqdm(dataloader, desc="Validation"):
            # Move to device
            mel_specs = mel_specs.to(device)
            onset_targets = onset_targets.to(device)
            offset_targets = offset_targets.to(device)
            frame_targets = frame_targets.to(device)
            velocity_targets = velocity_targets.to(device)
            mask = mask.to(device)

            # Forward pass
            onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)

            # Compute loss with mask
            loss, loss_dict = criterion(
                onset_pred, offset_pred, frame_pred, velocity_pred,
                onset_targets, offset_targets, frame_targets, velocity_targets,
                mask=mask
            )

            # Accumulate losses
            total_loss += loss_dict['total']
            total_onset_loss += loss_dict['onset']
            total_offset_loss += loss_dict['offset']
            total_frame_loss += loss_dict['frame']
            total_velocity_loss += loss_dict['velocity']
            num_batches += 1
            
            # Collect predictions and labels for metrics
            all_onset_probs.append(torch.sigmoid(onset_pred).cpu())
            all_offset_probs.append(torch.sigmoid(offset_pred).cpu())
            all_frame_probs.append(torch.sigmoid(frame_pred).cpu())
            all_velocity_preds.append(torch.sigmoid(velocity_pred).cpu())
            all_onset_labels.append(onset_targets.cpu())
            all_offset_labels.append(offset_targets.cpu())
            all_frame_labels.append(frame_targets.cpu())
            all_velocity_labels.append(velocity_targets.cpu())
            all_masks.append(mask.cpu())

    # Compute averages
    metrics = {
        'loss': total_loss / num_batches,
        'onset_loss': total_onset_loss / num_batches,
        'offset_loss': total_offset_loss / num_batches,
        'frame_loss': total_frame_loss / num_batches,
        'velocity_loss': total_velocity_loss / num_batches
    }
    
    # Compute evaluation metrics
    try:
        onset_probs = torch.cat(all_onset_probs, dim=0)
        offset_probs = torch.cat(all_offset_probs, dim=0)
        frame_probs = torch.cat(all_frame_probs, dim=0)
        velocity_preds = torch.cat(all_velocity_preds, dim=0)
        onset_labels = torch.cat(all_onset_labels, dim=0)
        offset_labels = torch.cat(all_offset_labels, dim=0)
        frame_labels = torch.cat(all_frame_labels, dim=0)
        velocity_labels = torch.cat(all_velocity_labels, dim=0)
        mask_combined = torch.cat(all_masks, dim=0)
        
        eval_metrics = compute_all_metrics(
            onset_probs, offset_probs, frame_probs, velocity_preds,
            onset_labels, offset_labels, frame_labels, velocity_labels,
            mask=mask_combined
        )
        
        metrics['eval_metrics'] = eval_metrics
    except Exception as e:
        print(f"Warning: Could not compute evaluation metrics: {e}")
    
    return metrics


def save_checkpoint(
    model: nn.Module,
    optimizer: torch.optim.Optimizer,
    epoch: int,
    metrics: Dict[str, float],
    output_dir: str,
    filename: str = None
):
    """Save model checkpoint."""
    if filename is None:
        filename = f"checkpoint_epoch_{epoch:03d}.pt"

    checkpoint = {
        'epoch': epoch,
        'model_state_dict': model.state_dict(),
        'optimizer_state_dict': optimizer.state_dict(),
        'metrics': metrics
    }

    output_path = Path(output_dir) / filename
    torch.save(checkpoint, output_path)
    print(f"Saved checkpoint: {output_path}")


def load_checkpoint(model: nn.Module, optimizer: torch.optim.Optimizer, checkpoint_path: str):
    """Load model checkpoint."""
    checkpoint = torch.load(checkpoint_path)

    model.load_state_dict(checkpoint['model_state_dict'])
    optimizer.load_state_dict(checkpoint['optimizer_state_dict'])

    return checkpoint['epoch'], checkpoint['metrics']


def main():
    parser = argparse.ArgumentParser(description='Train Onsets and Frames model')

    # Data
    parser.add_argument('--data-dir', type=str, required=True,
                        help='Directory containing processed data')
    parser.add_argument('--output-dir', type=str, default='models',
                        help='Output directory for checkpoints')
    parser.add_argument('--num-workers', type=int, default=None,
                        help='Number of data loading workers (default: auto-detect, max 8)')

    # Training
    parser.add_argument('--epochs', type=int, default=100,
                        help='Number of epochs')
    parser.add_argument('--batch-size', type=int, default=8,
                        help='Batch size')
    parser.add_argument('--sequence-length', type=int, default=2000,
                        help='Sequence length in frames (default: 2000, approx 20s)')
    parser.add_argument('--no-sliding-window', dest='sliding_window', action='store_false', default=True,
                        help='Disable sliding window sampling (use single random crop per file). Default: sliding window ENABLED for 9x more samples per epoch.')
    parser.add_argument('--window-stride', type=int, default=500,
                        help='Stride for sliding window in frames (default: 500). Only used when sliding window is enabled.')
    parser.add_argument('--learning-rate', type=float, default=0.0006,
                        help='Learning rate')
    parser.add_argument('--device', type=str, default='cuda',
                        choices=['cuda', 'cpu'],
                        help='Device to use (cuda or cpu)')

    # Model
    parser.add_argument('--resume', type=str, default=None,
                        help='Path to checkpoint to resume from')
    
    # Loss weights
    parser.add_argument('--onset-weight', type=float, default=1.0,
                        help='Weight for onset loss (default: 1.0)')
    parser.add_argument('--offset-weight', type=float, default=1.0,
                        help='Weight for offset loss (default: 1.0)')
    parser.add_argument('--frame-weight', type=float, default=1.0,
                        help='Weight for frame loss (default: 1.0)')
    parser.add_argument('--velocity-weight', type=float, default=1.0,
                        help='Weight for velocity loss (default: 1.0)')
    parser.add_argument('--onset-pos-weight', type=float, default=100.0,
                        help='Positive class weight for onset detection, compensates for extreme class imbalance in MAESTRO (~410:1 ratio). (default: 100.0)')
    parser.add_argument('--offset-pos-weight', type=float, default=100.0,
                        help='Positive class weight for offset detection, compensates for extreme class imbalance in MAESTRO (~412:1 ratio). (default: 100.0)')
    parser.add_argument('--frame-pos-weight', type=float, default=40.0,
                        help='Positive class weight for frame activation, compensates for class imbalance in MAESTRO (~33:1 ratio). (default: 40.0)')
    
    # Reproducibility
    parser.add_argument('--seed', type=int, default=42,
                        help='Random seed for reproducibility (default: 42)')

    # Logging
    parser.add_argument('--log-dir', type=str, default='runs',
                        help='TensorBoard log directory')
    parser.add_argument('--save-interval', type=int, default=10,
                        help='Save checkpoint every N epochs')
    
    # Early stopping
    parser.add_argument('--early-stopping-patience', type=int, default=15,
                        help='Patience for early stopping (stop if no improvement for N epochs). Set to 0 to disable. (default: 15)')
    parser.add_argument('--early-stopping-min-delta', type=float, default=0.001,
                        help='Minimum loss improvement to reset early stopping counter (default: 0.001)')

    args = parser.parse_args()

    # Create output directories
    Path(args.output_dir).mkdir(parents=True, exist_ok=True)
    Path(args.log_dir).mkdir(parents=True, exist_ok=True)

    # Set device
    if args.device == 'cuda' and not torch.cuda.is_available():
        print("CUDA not available, falling back to CPU")
        args.device = 'cpu'

    print(f"Using device: {args.device}")
    
    # Set random seeds for reproducibility
    set_seed(args.seed)
    print(f"Random seed set to: {args.seed}")
    
    # Auto-detect num_workers if not specified
    if args.num_workers is None:
        args.num_workers = min(8, os.cpu_count() or 1)
    print(f"Using {args.num_workers} data loading workers")

    # Create datasets
    print("\nLoading datasets...")
    train_dataset = MaestroDataset(args.data_dir, split='train', sequence_length=args.sequence_length)
    val_dataset = MaestroDataset(args.data_dir, split='validation', sequence_length=args.sequence_length)
    
    # Optionally wrap with sliding window for better data utilization
    if args.sliding_window:
        print(f"\nEnabling sliding window sampling with stride={args.window_stride}")
        train_dataset = SlidingWindowDataset(train_dataset, stride=args.window_stride, use_all_windows=True)
        # Note: validation dataset typically uses single crops for consistency
        val_dataset = SlidingWindowDataset(val_dataset, stride=args.window_stride, use_all_windows=False)

    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=args.num_workers,
        collate_fn=collate_fn,
        pin_memory=(args.device == 'cuda')
    )

    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=args.num_workers,
        collate_fn=collate_fn,
        pin_memory=(args.device == 'cuda')
    )

    # Create model
    print("\nCreating model...")
    model = create_model(args.device)
    print(f"Model parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Loss and optimizer
    criterion = OnsetsAndFramesLoss(
        onset_weight=args.onset_weight,
        offset_weight=args.offset_weight,
        frame_weight=args.frame_weight,
        velocity_weight=args.velocity_weight,
        onset_pos_weight=args.onset_pos_weight,
        offset_pos_weight=args.offset_pos_weight,
        frame_pos_weight=args.frame_pos_weight
    )

    optimizer = torch.optim.Adam(
        model.parameters(),
        lr=args.learning_rate,
        weight_decay=1e-5
    )

    # Learning rate scheduler
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
        optimizer,
        mode='min',
        factor=0.5,
        patience=5
    )

    # Mixed precision scaler
    scaler = torch.cuda.amp.GradScaler() if args.device == 'cuda' else None

    # Resume from checkpoint if specified
    start_epoch = 1
    if args.resume:
        print(f"\nResuming from checkpoint: {args.resume}")
        start_epoch, metrics = load_checkpoint(model, optimizer, args.resume)
        start_epoch += 1
        print(f"Resuming from epoch {start_epoch}")
        print(f"Previous metrics: {metrics}")

    # TensorBoard writer
    writer = SummaryWriter(args.log_dir)

    # Initialize early stopping if enabled
    early_stopping = None
    if args.early_stopping_patience > 0:
        early_stopping = EarlyStopping(
            patience=args.early_stopping_patience,
            min_delta=args.early_stopping_min_delta
        )
        print(f"\nEarly stopping enabled (patience={args.early_stopping_patience}, min_delta={args.early_stopping_min_delta})")
    else:
        print("\nEarly stopping disabled")

    # Training loop
    print("\nStarting training...")
    best_val_loss = float('inf')

    try:
        for epoch in range(start_epoch, args.epochs + 1):
            print(f"\n{'='*60}")
            print(f"Epoch {epoch}/{args.epochs}")
            print(f"{'='*60}")

            # Train
            train_metrics = train_epoch(
                model, train_loader, criterion, optimizer, args.device, epoch, writer, scaler
            )

            print(f"\nTrain metrics:")
            print(f"  Loss: {train_metrics['loss']:.4f}")
            print(f"  Onset: {train_metrics['onset_loss']:.4f}")
            print(f"  Offset: {train_metrics['offset_loss']:.4f}")
            print(f"  Frame: {train_metrics['frame_loss']:.4f}")
            print(f"  Velocity: {train_metrics['velocity_loss']:.4f}")

            # Validate
            val_metrics = validate(model, val_loader, criterion, args.device)

            print(f"\nValidation metrics:")
            print(f"  Loss: {val_metrics['loss']:.4f}")
            print(f"  Onset: {val_metrics['onset_loss']:.4f}")
            print(f"  Offset: {val_metrics['offset_loss']:.4f}")
            print(f"  Frame: {val_metrics['frame_loss']:.4f}")
            print(f"  Velocity: {val_metrics['velocity_loss']:.4f}")
            
            # Print evaluation metrics if available
            if 'eval_metrics' in val_metrics:
                print("\n  Evaluation Metrics:")
                print_metrics(val_metrics['eval_metrics'])

            # TensorBoard logging
            writer.add_scalar('train/epoch_loss', train_metrics['loss'], epoch)
            writer.add_scalar('val/epoch_loss', val_metrics['loss'], epoch)
            writer.add_scalar('val/onset_loss', val_metrics['onset_loss'], epoch)
            writer.add_scalar('val/offset_loss', val_metrics['offset_loss'], epoch)
            writer.add_scalar('val/frame_loss', val_metrics['frame_loss'], epoch)
            writer.add_scalar('val/velocity_loss', val_metrics['velocity_loss'], epoch)
            
            # Log evaluation metrics if available
            if 'eval_metrics' in val_metrics:
                for metric_name, metric_value in val_metrics['eval_metrics'].items():
                    if isinstance(metric_value, (int, float)):
                        writer.add_scalar(f'val/{metric_name}', metric_value, epoch)

            # Learning rate scheduling
            scheduler.step(val_metrics['loss'])

            # Save checkpoint
            if epoch % args.save_interval == 0:
                save_checkpoint(model, optimizer, epoch, val_metrics, args.output_dir)

            # Save best model
            if val_metrics['loss'] < best_val_loss:
                best_val_loss = val_metrics['loss']
                save_checkpoint(model, optimizer, epoch, val_metrics, args.output_dir, 'best_model.pt')
                print(f"  ✓ New best model saved! (loss: {best_val_loss:.4f})")
            
            # Check early stopping
            if early_stopping:
                early_stopping_status = early_stopping(val_metrics['loss'], epoch)
                print(f"  Early stopping: {early_stopping.counter}/{early_stopping.patience} epochs without improvement")
                
                if early_stopping_status:
                    print(f"\n{'='*60}")
                    print(f"Early stopping triggered!")
                    print(f"Best validation loss: {early_stopping.best_loss:.4f} (epoch {early_stopping.best_epoch})")
                    print(f"{'='*60}\n")
                    break

        print("\nTraining complete!")
    
    except KeyboardInterrupt:
        print("\n\n" + "="*60)
        print("Training interrupted by user (CTRL-C)")
        print("="*60)
        print("\nCleaning up resources...")
        
        # Save checkpoint before exiting
        save_checkpoint(model, optimizer, epoch, val_metrics if 'val_metrics' in locals() else {}, 
                       args.output_dir, f'interrupted_epoch_{epoch}.pt')
        print(f"✓ Saved checkpoint: interrupted_epoch_{epoch}.pt")
    
    finally:
        # Always clean up resources
        print("Closing TensorBoard writer...")
        writer.close()
        
        # Clear CUDA cache if using GPU
        if args.device == 'cuda':
            torch.cuda.empty_cache()
        
        print("✓ Cleanup complete\n")


if __name__ == '__main__':
    main()
