"""
Extract and display metrics from saved checkpoints.

This script reads the metrics that were stored in each checkpoint during training,
without needing to re-run evaluation.

Usage:
    python extract_checkpoint_metrics.py --start-epoch 40
    python extract_checkpoint_metrics.py --start-epoch 40 --models-dir models
"""

import argparse
import re
from pathlib import Path
import torch


def get_checkpoint_epoch(checkpoint_path: Path) -> int:
    """Extract epoch number from checkpoint filename."""
    match = re.search(r'epoch_(\d+)', checkpoint_path.name)
    if match:
        return int(match.group(1))
    return -1


def print_metrics(metrics: dict, epoch: int, checkpoint_name: str):
    """Print metrics in a formatted way."""
    print(f"\n{'='*60}")
    print(f"Checkpoint: {checkpoint_name} (Epoch {epoch})")
    print(f"{'='*60}")

    # Extract validation losses
    print(f"\nValidation Loss: {metrics.get('loss', 'N/A'):.4f}")
    print(f"  Onset Loss:    {metrics.get('onset_loss', 'N/A'):.4f}")
    print(f"  Offset Loss:   {metrics.get('offset_loss', 'N/A'):.4f}")
    print(f"  Frame Loss:    {metrics.get('frame_loss', 'N/A'):.4f}")
    print(f"  Velocity Loss: {metrics.get('velocity_loss', 'N/A'):.4f}")

    # Extract evaluation metrics if available
    eval_metrics = metrics.get('eval_metrics', {})

    if eval_metrics:
        print("\nEvaluation Metrics:")
        print("="*60)

        # Onset metrics
        print("\nOnset Detection (Frame-Level):")
        print(f"  onset_precision     : {eval_metrics.get('onset_precision', 0.0):.4f}")
        print(f"  onset_recall        : {eval_metrics.get('onset_recall', 0.0):.4f}")
        print(f"  onset_f1            : {eval_metrics.get('onset_f1', 0.0):.4f}")
        print(f"  onset_accuracy      : {eval_metrics.get('onset_accuracy', 0.0):.4f}")

        # Offset metrics
        print("\nOffset Detection (Frame-Level):")
        print(f"  offset_precision    : {eval_metrics.get('offset_precision', 0.0):.4f}")
        print(f"  offset_recall       : {eval_metrics.get('offset_recall', 0.0):.4f}")
        print(f"  offset_f1           : {eval_metrics.get('offset_f1', 0.0):.4f}")
        print(f"  offset_accuracy     : {eval_metrics.get('offset_accuracy', 0.0):.4f}")

        # Frame metrics
        print("\nFrame Predictions (Frame-Level):")
        print(f"  frame_precision     : {eval_metrics.get('frame_precision', 0.0):.4f}")
        print(f"  frame_recall        : {eval_metrics.get('frame_recall', 0.0):.4f}")
        print(f"  frame_f1            : {eval_metrics.get('frame_f1', 0.0):.4f}")
        print(f"  frame_accuracy      : {eval_metrics.get('frame_accuracy', 0.0):.4f}")

        # Velocity
        print(f"\nVelocity MAE (Frame-Level): {eval_metrics.get('velocity_mae', 0.0):.4f}")

        # Note-level metrics (most important)
        if 'note_f1' in eval_metrics:
            print("\n" + "="*60)
            print("NOTE-LEVEL METRICS (Most Important for Transcription Quality):")
            print("="*60)
            print(f"\n  Precision: {eval_metrics.get('note_precision', 0.0):.4f}")
            print(f"  Recall:    {eval_metrics.get('note_recall', 0.0):.4f}")
            print(f"  F1 Score:  {eval_metrics.get('note_f1', 0.0):.4f}")

            if 'num_notes_pred' in eval_metrics and 'num_notes_ref' in eval_metrics:
                print(f"\n  Predicted Notes: {int(eval_metrics['num_notes_pred']):4d}")
                print(f"  Reference Notes: {int(eval_metrics['num_notes_ref']):4d}")


def main():
    parser = argparse.ArgumentParser(description='Extract metrics from checkpoints')

    parser.add_argument('--models-dir', type=str, default='models',
                        help='Directory containing checkpoint files')
    parser.add_argument('--start-epoch', type=int, default=40,
                        help='Starting epoch number (default: 40)')
    parser.add_argument('--device', type=str, default='cpu',
                        choices=['cuda', 'cpu'],
                        help='Device to load checkpoints on (default: cpu)')

    args = parser.parse_args()

    # Find all checkpoints
    models_dir = Path(args.models_dir)
    checkpoint_files = sorted(models_dir.glob('checkpoint_epoch_*.pt'))

    # Filter checkpoints >= start_epoch
    checkpoints_to_process = [
        cp for cp in checkpoint_files
        if get_checkpoint_epoch(cp) >= args.start_epoch
    ]

    # Sort by epoch number
    checkpoints_to_process.sort(key=get_checkpoint_epoch)

    print(f"Found {len(checkpoints_to_process)} checkpoints to process (epochs >= {args.start_epoch})")
    print("Checkpoints:")
    for cp in checkpoints_to_process:
        print(f"  - {cp.name} (epoch {get_checkpoint_epoch(cp)})")

    if not checkpoints_to_process:
        print(f"\nNo checkpoints found in {models_dir} with epoch >= {args.start_epoch}")
        return

    # Extract metrics from each checkpoint
    all_results = []

    for checkpoint_path in checkpoints_to_process:
        epoch = get_checkpoint_epoch(checkpoint_path)

        try:
            # Load checkpoint (weights_only=False to load metrics dict)
            checkpoint = torch.load(checkpoint_path, map_location=args.device, weights_only=False)
            metrics = checkpoint.get('metrics', {})

            # Print detailed metrics
            print_metrics(metrics, epoch, checkpoint_path.name)

            # Store for summary
            all_results.append({
                'epoch': epoch,
                'checkpoint': checkpoint_path.name,
                'metrics': metrics
            })

        except Exception as e:
            print(f"\n⚠️  Error loading {checkpoint_path.name}: {e}")
            continue

    # Print summary table
    print("\n" + "="*80)
    print("SUMMARY OF ALL CHECKPOINTS")
    print("="*80)

    print("\nEpoch | Val Loss | Note F1  | Note Prec | Note Recall | Frame F1 | Onset F1")
    print("-" * 80)

    for result in all_results:
        epoch = result['epoch']
        metrics = result['metrics']
        loss = metrics.get('loss', 0.0)

        eval_metrics = metrics.get('eval_metrics', {})
        note_f1 = eval_metrics.get('note_f1', 0.0)
        note_prec = eval_metrics.get('note_precision', 0.0)
        note_recall = eval_metrics.get('note_recall', 0.0)
        frame_f1 = eval_metrics.get('frame_f1', 0.0)
        onset_f1 = eval_metrics.get('onset_f1', 0.0)

        print(f"{epoch:5d} | {loss:8.4f} | {note_f1:8.4f} | {note_prec:9.4f} | {note_recall:11.4f} | {frame_f1:8.4f} | {onset_f1:8.4f}")

    print("="*80)

    # Find best checkpoint by note F1
    if all_results:
        best_by_note_f1 = max(all_results, key=lambda r: r['metrics'].get('eval_metrics', {}).get('note_f1', 0.0))
        best_epoch = best_by_note_f1['epoch']
        best_note_f1 = best_by_note_f1['metrics'].get('eval_metrics', {}).get('note_f1', 0.0)

        print(f"\nBest checkpoint by Note F1: Epoch {best_epoch} (F1 = {best_note_f1:.4f})")


if __name__ == '__main__':
    main()
