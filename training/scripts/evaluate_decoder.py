import torch
import argparse
import json
from torch.utils.data import DataLoader
from tqdm import tqdm

# Import your existing code
from train import MaestroDataset, collate_fn
from model import create_model
from metrics import compute_note_metrics, compute_onset_metrics, compute_offset_metrics

# CONFIG
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'
CHECKPOINT = "models/best_f1.pt"
DATA_DIR = r"..\tmp\maestro-v3.0.0-processed"
SAMPLE_RATE = 16000
HOP_SIZE = 512
FPS = SAMPLE_RATE / HOP_SIZE  # ~31.25

def evaluate_split(model, dataloader, params, split_name, num_batches):
    """
    Evaluate the model on a dataset split with given parameters.

    Processes each batch individually and computes metrics sample-by-sample to minimize memory usage.

    Args:
        model: The trained model
        dataloader: DataLoader for the split
        params: Dictionary of decoder parameters
        split_name: Name of the split (for logging)
        num_batches: Total number of batches for progress tracking

    Returns:
        Dictionary with aggregated metrics
    """
    model.eval()

    # Accumulate metrics sample-by-sample
    total_metrics = {
        'note_f1': 0,
        'note_precision': 0,
        'note_recall': 0,
        'onset_f1': 0,
        'onset_precision': 0,
        'onset_recall': 0,
        'offset_f1': 0,
        'offset_precision': 0,
        'offset_recall': 0
    }
    num_samples = 0

    print(f"\nEvaluating {split_name} set ({num_batches} batches, processing sample-by-sample)...")

    with torch.no_grad():
        for batch_idx, batch in enumerate(tqdm(dataloader, desc=f"{split_name}", total=num_batches)):
            mel, onset_ref, offset_ref, frame_ref, vel_ref, mask = batch
            # Ensure mel is contiguous before passing to model
            mel = mel.to(DEVICE).contiguous()

            # Inference with retry on cuDNN error
            max_retries = 3
            skip_batch = False
            for retry in range(max_retries):
                try:
                    o_pred, off_pred, f_pred, v_pred = model(mel)
                    break  # Success, exit retry loop
                except RuntimeError as e:
                    if "cuDNN" in str(e):
                        if retry < max_retries - 1:
                            # cuDNN error - try clearing cache and retrying
                            print(f"\nWarning: cuDNN error at batch {batch_idx}, retry {retry + 1}/{max_retries}")
                            if DEVICE == 'cuda':
                                torch.cuda.empty_cache()
                                torch.cuda.synchronize()
                            continue
                        else:
                            # Out of retries - skip this batch
                            print(f"\nSkipping batch {batch_idx} due to persistent cuDNN error after {max_retries} retries")
                            skip_batch = True
                            break
                    else:
                        # Not a cuDNN error
                        raise

            if skip_batch:
                continue  # Skip to next batch

            # Sigmoid and move to CPU immediately to free GPU memory
            onset_probs = torch.sigmoid(o_pred).cpu()
            offset_probs = torch.sigmoid(off_pred).cpu()
            frame_probs = torch.sigmoid(f_pred).cpu()
            velocity_preds = torch.sigmoid(v_pred).cpu()

            # Move labels to CPU
            onset_ref = onset_ref.cpu()
            offset_ref = offset_ref.cpu()
            frame_ref = frame_ref.cpu()
            vel_ref = vel_ref.cpu()
            mask = mask.cpu()

            # Process each sample in the batch individually
            batch_size = mel.shape[0]
            for i in range(batch_size):
                # Extract single sample using indexing (creates contiguous tensors)
                sample_onset_probs = onset_probs[i:i+1].clone()
                sample_offset_probs = offset_probs[i:i+1].clone()
                sample_frame_probs = frame_probs[i:i+1].clone()
                sample_velocity_preds = velocity_preds[i:i+1].clone()
                sample_onset_labels = onset_ref[i:i+1].clone()
                sample_offset_labels = offset_ref[i:i+1].clone()
                sample_frame_labels = frame_ref[i:i+1].clone()
                sample_velocity_labels = vel_ref[i:i+1].clone()
                sample_mask = mask[i:i+1].clone()

                # Compute metrics for this single sample
                sample_metrics = compute_note_metrics(
                    sample_onset_probs, sample_offset_probs, sample_frame_probs, sample_velocity_preds,
                    sample_onset_labels, sample_offset_labels, sample_frame_labels, sample_velocity_labels,
                    onset_thresh=params['onset_thresh'],
                    frame_thresh=params['frame_thresh'],
                    offset_thresh=params['offset_thresh'],
                    min_duration_seconds=params['min_duration'],
                    gap_tolerance_seconds=params['gap_tolerance'],
                    min_velocity=params['min_velocity'],
                    min_frame_for_onset=params['min_frame_for_onset'],
                    frame_rate=FPS,
                    mask=sample_mask
                )

                # Compute onset/offset metrics separately (frame-level)
                onset_metrics = compute_onset_metrics(
                    sample_onset_probs, sample_onset_labels,
                    threshold=params['onset_thresh'],
                    mask=sample_mask
                )
                offset_metrics = compute_offset_metrics(
                    sample_offset_probs, sample_offset_labels,
                    threshold=params['offset_thresh'],
                    mask=sample_mask
                )

                # Accumulate sample metrics
                for key in total_metrics.keys():
                    if key in sample_metrics:
                        total_metrics[key] += sample_metrics[key]
                    elif key in onset_metrics:
                        total_metrics[key] += onset_metrics[key]
                    elif key in offset_metrics:
                        total_metrics[key] += offset_metrics[key]
                num_samples += 1

            # Free memory after each batch
            del onset_probs, offset_probs, frame_probs, velocity_preds
            del o_pred, off_pred, f_pred, v_pred
            if DEVICE == 'cuda':
                torch.cuda.empty_cache()

    # Average metrics across all samples
    avg_metrics = {k: v / num_samples if num_samples > 0 else 0 for k, v in total_metrics.items()}

    return avg_metrics

def main():
    parser = argparse.ArgumentParser(description='Evaluate decoder parameters on training and validation sets')
    parser.add_argument('--onset_thresh', type=float, default=0.46634514588306974)
    parser.add_argument('--frame_thresh', type=float, default=0.8946657918559437)
    parser.add_argument('--offset_thresh', type=float, default=0.8307139579972912)
    parser.add_argument('--min_velocity', type=float, default=0.017537805373506878)
    parser.add_argument('--min_duration', type=float, default=0.050547378558136784)
    parser.add_argument('--gap_tolerance', type=float, default=0.15503368741936713)
    parser.add_argument('--min_frame_for_onset', type=float, default=0.463827429076491)
    parser.add_argument('--checkpoint', type=str, default=CHECKPOINT, help='Path to model checkpoint')
    parser.add_argument('--data_dir', type=str, default=DATA_DIR, help='Path to processed data')
    parser.add_argument('--output', type=str, default=None, help='Save results to JSON file')
    parser.add_argument('--batch_size', type=int, default=1, help='Batch size for evaluation (default: 1, recommended to avoid cuDNN errors)')
    parser.add_argument('--train', action='store_true', help='Evaluate on training set')
    parser.add_argument('--validation', action='store_true', help='Evaluate on validation set')
    parser.add_argument('--test', action='store_true', help='Evaluate on test set')
    
    args = parser.parse_args()
    
    # If no split specified, evaluate on all
    if not (args.train or args.validation or args.test):
        args.train = True
        args.validation = True
        args.test = True
    
    # Collect parameters
    params = {
        'onset_thresh': args.onset_thresh,
        'frame_thresh': args.frame_thresh,
        'offset_thresh': args.offset_thresh,
        'min_velocity': args.min_velocity,
        'min_duration': args.min_duration,
        'gap_tolerance': args.gap_tolerance,
        'min_frame_for_onset': args.min_frame_for_onset
    }
    
    print("="*60)
    print("DECODER PARAMETER EVALUATION")
    print("="*60)
    print(f"Device: {DEVICE}")
    print(f"Checkpoint: {args.checkpoint}")
    print(f"Data Directory: {args.data_dir}")
    print("\nParameters:")
    for k, v in params.items():
        print(f"  {k}: {v}")
    print("="*60)
    
    # Load Model
    model = create_model(DEVICE)
    print(f"\nLoading checkpoint: {args.checkpoint}")
    checkpoint = torch.load(args.checkpoint, map_location=DEVICE)
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    
    results = {
        'parameters': params,
        'checkpoint': args.checkpoint,
        'splits': {}
    }
    
    # Evaluate on requested splits
    if args.train:
        train_dataset = MaestroDataset(args.data_dir, split='train')
        train_loader = DataLoader(train_dataset, batch_size=args.batch_size, collate_fn=collate_fn, 
                                 num_workers=4, shuffle=False)
        train_metrics = evaluate_split(model, train_loader, params, 'Train', len(train_loader))
        results['splits']['train'] = train_metrics
    
    if args.validation:
        val_dataset = MaestroDataset(args.data_dir, split='validation')
        val_loader = DataLoader(val_dataset, batch_size=args.batch_size, collate_fn=collate_fn, 
                               num_workers=4, shuffle=False)
        val_metrics = evaluate_split(model, val_loader, params, 'Validation', len(val_loader))
        results['splits']['validation'] = val_metrics
    
    if args.test:
        test_dataset = MaestroDataset(args.data_dir, split='test')
        test_loader = DataLoader(test_dataset, batch_size=args.batch_size, collate_fn=collate_fn, 
                                num_workers=4, shuffle=False)
        test_metrics = evaluate_split(model, test_loader, params, 'Test', len(test_loader))
        results['splits']['test'] = test_metrics
    
    # Print Results
    print("\n" + "="*60)
    print("RESULTS")
    print("="*60)
    
    for split_name, metrics in results['splits'].items():
        print(f"\n{split_name.upper()} SET:")
        print(f"  Note      - P: {metrics['note_precision']:.4f}  R: {metrics['note_recall']:.4f}  F1: {metrics['note_f1']:.4f}")
        print(f"  Onset     - P: {metrics['onset_precision']:.4f}  R: {metrics['onset_recall']:.4f}  F1: {metrics['onset_f1']:.4f}")
        print(f"  Offset    - P: {metrics['offset_precision']:.4f}  R: {metrics['offset_recall']:.4f}  F1: {metrics['offset_f1']:.4f}")
    
    # Save to file if requested
    if args.output:
        with open(args.output, 'w') as f:
            json.dump(results, f, indent=2)
        print(f"\nResults saved to: {args.output}")
    
    print("="*60)

if __name__ == '__main__':
    main()
