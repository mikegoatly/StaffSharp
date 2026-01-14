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
        # Load preprocessed data
        data = np.load(self.files[idx])

        # Get raw arrays first
        m_spec = data['mel_spec']
        o_roll = data['onset_roll']
        p_roll = data['piano_roll']
        v_roll = data['velocity_roll']

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
            p_roll = p_roll[start:end]
            v_roll = v_roll[start:end]

        mel_spec = torch.from_numpy(m_spec)
        onset_roll = torch.from_numpy(o_roll)
        piano_roll = torch.from_numpy(p_roll)
        velocity_roll = torch.from_numpy(v_roll)

        # Derive offset roll from piano roll
        # Offset is 1 at frame t if piano_roll[t-1] == 1 and piano_roll[t] == 0
        # We can implement this simply by checking transitions
        offset_roll = torch.zeros_like(piano_roll)
        
        # Shift piano roll right by 1 to compare t and t-1
        # Pad with 0 at the beginning
        p_shifted = torch.roll(piano_roll, 1, 0)
        p_shifted[0, :] = 0
        
        # Check where note ends: active at t-1 and inactive at t
        offset_mask = (p_shifted == 1) & (piano_roll == 0)
        offset_roll[offset_mask] = 1.0

        return mel_spec, onset_roll, offset_roll, piano_roll, velocity_roll


def collate_fn(batch):
    """
    Collate function to handle variable-length sequences.
    Pads sequences to the longest in the batch and returns a mask.
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
    """Validate the model."""
    model.eval()

    total_loss = 0
    total_onset_loss = 0
    total_frame_loss = 0
    total_velocity_loss = 0
    num_batches = 0

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
            total_frame_loss += loss_dict['frame']
            total_velocity_loss += loss_dict['velocity']
            num_batches += 1

    # Compute averages
    metrics = {
        'loss': total_loss / num_batches,
        'onset_loss': total_onset_loss / num_batches,
        'frame_loss': total_frame_loss / num_batches,
        'velocity_loss': total_velocity_loss / num_batches
    }

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

    # Training
    parser.add_argument('--epochs', type=int, default=100,
                        help='Number of epochs')
    parser.add_argument('--batch-size', type=int, default=8,
                        help='Batch size')
    parser.add_argument('--sequence-length', type=int, default=2000,
                        help='Sequence length in frames (default: 2000, approx 20s)')
    parser.add_argument('--learning-rate', type=float, default=0.0006,
                        help='Learning rate')
    parser.add_argument('--device', type=str, default='cuda',
                        choices=['cuda', 'cpu'],
                        help='Device to use (cuda or cpu)')

    # Model
    parser.add_argument('--resume', type=str, default=None,
                        help='Path to checkpoint to resume from')

    # Logging
    parser.add_argument('--log-dir', type=str, default='runs',
                        help='TensorBoard log directory')
    parser.add_argument('--save-interval', type=int, default=10,
                        help='Save checkpoint every N epochs')

    args = parser.parse_args()

    # Create output directories
    Path(args.output_dir).mkdir(parents=True, exist_ok=True)
    Path(args.log_dir).mkdir(parents=True, exist_ok=True)

    # Set device
    if args.device == 'cuda' and not torch.cuda.is_available():
        print("CUDA not available, falling back to CPU")
        args.device = 'cpu'

    print(f"Using device: {args.device}")

    # Create datasets
    print("\nLoading datasets...")
    train_dataset = MaestroDataset(args.data_dir, split='train', sequence_length=args.sequence_length)
    val_dataset = MaestroDataset(args.data_dir, split='validation', sequence_length=args.sequence_length)

    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        num_workers=2,
        collate_fn=collate_fn,
        pin_memory=(args.device == 'cuda')
    )

    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        num_workers=2,
        collate_fn=collate_fn,
        pin_memory=(args.device == 'cuda')
    )

    # Create model
    print("\nCreating model...")
    model = create_model(args.device)
    print(f"Model parameters: {sum(p.numel() for p in model.parameters()):,}")

    # Loss and optimizer
    criterion = OnsetsAndFramesLoss(
        onset_weight=1.0,
        offset_weight=1.0,
        frame_weight=1.0,
        velocity_weight=1.0
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

    # Training loop
    print("\nStarting training...")
    best_val_loss = float('inf')

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
        print(f"  Frame: {train_metrics['frame_loss']:.4f}")
        print(f"  Velocity: {train_metrics['velocity_loss']:.4f}")

        # Validate
        val_metrics = validate(model, val_loader, criterion, args.device)

        print(f"\nValidation metrics:")
        print(f"  Loss: {val_metrics['loss']:.4f}")
        print(f"  Onset: {val_metrics['onset_loss']:.4f}")
        print(f"  Frame: {val_metrics['frame_loss']:.4f}")
        print(f"  Velocity: {val_metrics['velocity_loss']:.4f}")

        # TensorBoard logging
        writer.add_scalar('train/epoch_loss', train_metrics['loss'], epoch)
        writer.add_scalar('val/epoch_loss', val_metrics['loss'], epoch)
        writer.add_scalar('val/onset_loss', val_metrics['onset_loss'], epoch)
        writer.add_scalar('val/frame_loss', val_metrics['frame_loss'], epoch)
        writer.add_scalar('val/velocity_loss', val_metrics['velocity_loss'], epoch)

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

    print("\nTraining complete!")
    writer.close()


if __name__ == '__main__':
    main()
