"""
Evaluation metrics for piano transcription.

Computes onset detection metrics (precision, recall, F1) and frame-level accuracy.
These metrics are computed on the validation set during training to monitor model performance
beyond just loss values.

IMPORTANT: Also includes note-level metrics computed by decoding frame predictions into discrete
note events using the same logic as the C# NoteEventDecoder. This ensures validation metrics
match the actual inference behavior.
"""

import numpy as np
import torch
from typing import Dict, List, Tuple


# Import the decoder that matches the C# implementation
import sys
from pathlib import Path
try:
    from decode_notes import decode_notes
except ImportError:
    # Fallback if decode_notes isn't available
    decode_notes = None


def compute_onset_metrics(
    onset_probs: torch.Tensor,
    onset_labels: torch.Tensor,
    threshold: float = 0.5,
    mask: torch.Tensor = None
) -> Dict[str, float]:
    """
    Compute onset detection metrics (precision, recall, F1).
    
    Treats onset detection as a binary classification problem where:
    - Positive class: Note onset (onset_label == 1)
    - Negative class: No onset (onset_label == 0)
    
    Args:
        onset_probs: Predicted probabilities with shape (batch, time, 88)
        onset_labels: Ground truth binary labels with shape (batch, time, 88)
        threshold: Classification threshold (default: 0.5)
        mask: Optional binary mask for ignoring padding (batch, time)
    
    Returns:
        Dictionary with metrics:
            - onset_precision: True positives / (True positives + False positives)
            - onset_recall: True positives / (True positives + False negatives)
            - onset_f1: Harmonic mean of precision and recall
            - onset_accuracy: Correct predictions / Total predictions
    """
    # Convert predictions to binary
    onset_pred_binary = (onset_probs > threshold).float()
    
    # Apply mask if provided
    if mask is not None:
        # Mask shape: (batch, time) -> expand to (batch, time, 1) -> expand to (batch, time, 88)
        mask_expanded = mask.unsqueeze(-1).expand_as(onset_pred_binary)
        onset_pred_binary = onset_pred_binary * mask_expanded
        onset_labels = onset_labels * mask_expanded
    
    # Flatten for computation
    pred_flat = onset_pred_binary.reshape(-1)
    label_flat = onset_labels.reshape(-1)
    
    # Compute confusion matrix elements
    tp = ((pred_flat == 1) & (label_flat == 1)).sum().item()  # True positives
    fp = ((pred_flat == 1) & (label_flat == 0)).sum().item()  # False positives
    fn = ((pred_flat == 0) & (label_flat == 1)).sum().item()  # False negatives
    tn = ((pred_flat == 0) & (label_flat == 0)).sum().item()  # True negatives
    
    # Compute metrics
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0
    accuracy = (tp + tn) / (tp + tn + fp + fn) if (tp + tn + fp + fn) > 0 else 0.0
    
    return {
        'onset_precision': precision,
        'onset_recall': recall,
        'onset_f1': f1,
        'onset_accuracy': accuracy
    }


def compute_frame_metrics(
    frame_probs: torch.Tensor,
    frame_labels: torch.Tensor,
    threshold: float = 0.5,
    mask: torch.Tensor = None
) -> Dict[str, float]:
    """
    Compute frame-level metrics.
    
    Computes the same metrics as onset but for frame activations (note being active).
    Frames are typically easier to predict than onsets due to longer duration.
    
    Args:
        frame_probs: Predicted frame activation probabilities with shape (batch, time, 88)
        frame_labels: Ground truth frame labels with shape (batch, time, 88)
        threshold: Classification threshold (default: 0.5)
        mask: Optional binary mask for ignoring padding (batch, time)
    
    Returns:
        Dictionary with metrics: frame_precision, frame_recall, frame_f1, frame_accuracy
    """
    # Convert predictions to binary
    frame_pred_binary = (frame_probs > threshold).float()
    
    # Apply mask if provided
    if mask is not None:
        mask_expanded = mask.unsqueeze(-1).expand_as(frame_pred_binary)
        frame_pred_binary = frame_pred_binary * mask_expanded
        frame_labels = frame_labels * mask_expanded
    
    # Flatten for computation
    pred_flat = frame_pred_binary.reshape(-1)
    label_flat = frame_labels.reshape(-1)
    
    # Compute confusion matrix elements
    tp = ((pred_flat == 1) & (label_flat == 1)).sum().item()
    fp = ((pred_flat == 1) & (label_flat == 0)).sum().item()
    fn = ((pred_flat == 0) & (label_flat == 1)).sum().item()
    tn = ((pred_flat == 0) & (label_flat == 0)).sum().item()
    
    # Compute metrics
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0
    accuracy = (tp + tn) / (tp + tn + fp + fn) if (tp + tn + fp + fn) > 0 else 0.0
    
    return {
        'frame_precision': precision,
        'frame_recall': recall,
        'frame_f1': f1,
        'frame_accuracy': accuracy
    }


def compute_offset_metrics(
    offset_probs: torch.Tensor,
    offset_labels: torch.Tensor,
    threshold: float = 0.5,
    mask: torch.Tensor = None
) -> Dict[str, float]:
    """
    Compute offset detection metrics.
    
    Args:
        offset_probs: Predicted probabilities shape (batch, time, 88)
        offset_labels: Ground truth labels shape (batch, time, 88)
        threshold: Classification threshold (default: 0.5)
        mask: Optional mask for ignoring padding (batch, time)
    
    Returns:
        Dictionary with offset metrics (offset_precision, offset_recall, offset_f1, offset_accuracy)
    """
    offset_pred_binary = (offset_probs > threshold).float()
    
    if mask is not None:
        mask_expanded = mask.unsqueeze(-1).expand_as(offset_pred_binary)
        offset_pred_binary = offset_pred_binary * mask_expanded
        offset_labels = offset_labels * mask_expanded
    
    pred_flat = offset_pred_binary.reshape(-1)
    label_flat = offset_labels.reshape(-1)
    
    tp = ((pred_flat == 1) & (label_flat == 1)).sum().item()
    fp = ((pred_flat == 1) & (label_flat == 0)).sum().item()
    fn = ((pred_flat == 0) & (label_flat == 1)).sum().item()
    tn = ((pred_flat == 0) & (label_flat == 0)).sum().item()
    
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0
    accuracy = (tp + tn) / (tp + tn + fp + fn) if (tp + tn + fp + fn) > 0 else 0.0
    
    return {
        'offset_precision': precision,
        'offset_recall': recall,
        'offset_f1': f1,
        'offset_accuracy': accuracy
    }


def compute_velocity_mae(
    velocity_preds: torch.Tensor,
    velocity_labels: torch.Tensor,
    onset_labels: torch.Tensor = None,
    mask: torch.Tensor = None
) -> float:
    """
    Compute mean absolute error (MAE) of velocity predictions.
    
    Velocity is normalized to [0, 1] range. Only computed where onsets occur.
    
    Args:
        velocity_preds: Predicted velocities with shape (batch, time, 88)
        velocity_labels: Ground truth velocities with shape (batch, time, 88)
        onset_labels: Optional onset labels to only compute velocity error at onsets
        mask: Optional binary mask for ignoring padding (batch, time)
    
    Returns:
        Mean absolute error in velocity predictions
    """
    mae = torch.abs(velocity_preds - velocity_labels)
    
    # Only compute error where onsets occur if provided
    if onset_labels is not None:
        mae = mae * onset_labels
        num_onsets = onset_labels.sum().item()
        if num_onsets > 0:
            return (mae.sum() / num_onsets).item()
        else:
            return 0.0
    
    # Apply mask if provided
    if mask is not None:
        mask_expanded = mask.unsqueeze(-1).expand_as(mae)
        mae = mae * mask_expanded
        mask_count = mask_expanded.sum().item()
        if mask_count > 0:
            return (mae.sum() / mask_count).item()
    
    return mae.mean().item()


def compute_note_metrics(
    onset_probs: torch.Tensor,
    offset_probs: torch.Tensor,
    frame_probs: torch.Tensor,
    velocity_preds: torch.Tensor,
    onset_labels: torch.Tensor,
    offset_labels: torch.Tensor,
    frame_labels: torch.Tensor,
    velocity_labels: torch.Tensor,
    onset_thresh: float = 0.5,
    offset_thresh: float = 0.5,
    frame_thresh: float = 0.5,
    min_duration_seconds: float = 0.05,
    frame_rate: float = 100.0,
    mask: torch.Tensor = None
) -> Dict[str, float]:
    """
    Compute note-level metrics by decoding frame predictions into discrete notes.
    
    This evaluates the actual transcription quality by:
    1. Converting frame-level predictions to discrete note events using the C#-equivalent decoder
    2. Matching predicted notes to ground truth notes
    3. Computing note-level precision, recall, and F1
    
    This is MORE meaningful than frame-level metrics because it tests:
    - Correct note onset and offset detection
    - Minimum duration filtering
    - Note event reconstruction (not just frame classification)
    - Whether the C# decoder produces correct output
    
    Args:
        onset_probs: (batch, time, 88) - Predicted onset probabilities
        offset_probs: (batch, time, 88) - Predicted offset probabilities
        frame_probs: (batch, time, 88) - Predicted frame probabilities
        velocity_preds: (batch, time, 88) - Predicted velocities
        onset_labels: (batch, time, 88) - Ground truth onsets
        offset_labels: (batch, time, 88) - Ground truth offsets (not always available)
        frame_labels: (batch, time, 88) - Ground truth frames
        velocity_labels: (batch, time, 88) - Ground truth velocities
        onset_thresh: Threshold for onset detection (default: 0.5)
        offset_thresh: Threshold for offset detection (default: 0.5)
        frame_thresh: Threshold for frame activation (default: 0.5)
        min_duration_seconds: Minimum note duration (default: 0.05, matches C# default)
        frame_rate: Frame rate in Hz (default: 100.0)
        mask: Optional mask for padding
    
    Returns:
        Dictionary with note-level metrics:
            - note_precision: TP / (TP + FP)
            - note_recall: TP / (TP + FN)
            - note_f1: Harmonic mean of precision and recall
            - num_notes_pred: Total predicted notes
            - num_notes_ref: Total reference notes
    """
    if decode_notes is None:
        return {'note_f1': 0.0, 'note_precision': 0.0, 'note_recall': 0.0}
    
    batch_size = onset_probs.shape[0]
    
    all_pred_notes = []
    all_ref_notes = []
    
    # Process each sample in batch
    for batch_idx in range(batch_size):
        # Get this sample's predictions
        onset_pred = onset_probs[batch_idx].cpu().numpy()  # (time, 88)
        offset_pred = offset_probs[batch_idx].cpu().numpy()
        frame_pred = frame_probs[batch_idx].cpu().numpy()
        velocity_pred = velocity_preds[batch_idx].cpu().numpy()
        
        # Get this sample's references
        onset_ref = onset_labels[batch_idx].cpu().numpy()
        offset_ref = offset_labels[batch_idx].cpu().numpy()
        frame_ref = frame_labels[batch_idx].cpu().numpy()
        velocity_ref = velocity_labels[batch_idx].cpu().numpy()
        
        # Apply mask if provided (truncate to actual sequence length)
        if mask is not None:
            actual_length = int(mask[batch_idx].sum().item())
            onset_pred = onset_pred[:actual_length]
            offset_pred = offset_pred[:actual_length]
            frame_pred = frame_pred[:actual_length]
            velocity_pred = velocity_pred[:actual_length]
            onset_ref = onset_ref[:actual_length]
            offset_ref = offset_ref[:actual_length]
            frame_ref = frame_ref[:actual_length]
            velocity_ref = velocity_ref[:actual_length]
        
        # Decode predicted notes
        pred_notes = decode_notes(
            onset_pred, frame_pred, offset_pred, velocity_pred,
            onset_thresh=onset_thresh,
            frame_thresh=frame_thresh,
            offset_thresh=offset_thresh,
            min_duration_seconds=min_duration_seconds,
            frame_rate=frame_rate
        )
        
        # Decode reference notes from ground truth
        ref_notes = decode_notes(
            onset_ref, frame_ref, offset_ref, velocity_ref,
            onset_thresh=0.5,  # Use same threshold for reference
            frame_thresh=0.5,
            offset_thresh=0.5,
            min_duration_seconds=min_duration_seconds,
            frame_rate=frame_rate
        )
        
        all_pred_notes.append(pred_notes)
        all_ref_notes.append(ref_notes)
    
    # Compute note-level metrics (simple matching with time tolerance)
    tp = 0
    fp = 0
    fn = 0
    
    time_tolerance = 0.05  # 50ms tolerance for note matching
    
    for pred_notes, ref_notes in zip(all_pred_notes, all_ref_notes):
        matched_refs = set()
        
        # For each predicted note, find matching reference note
        for pred in pred_notes:
            matched = False
            for ref_idx, ref in enumerate(ref_notes):
                if ref_idx in matched_refs:
                    continue
                
                # Match if pitch and timing (onset and offset) are close enough
                pitch_match = pred['pitch'] == ref['pitch']
                onset_match = abs(pred['start'] - ref['start']) <= time_tolerance
                offset_match = abs(pred['end'] - ref['end']) <= time_tolerance
                
                if pitch_match and onset_match and offset_match:

                    tp += 1
                    matched_refs.add(ref_idx)
                    matched = True
                    break
            
            if not matched:
                fp += 1
        
        # Remaining unmatched references are false negatives
        fn += len(ref_notes) - len(matched_refs)
    
    # Compute metrics
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    recall = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0
    
    total_pred = sum(len(notes) for notes in all_pred_notes)
    total_ref = sum(len(notes) for notes in all_ref_notes)
    
    return {
        'note_precision': precision,
        'note_recall': recall,
        'note_f1': f1,
        'num_notes_pred': total_pred,
        'num_notes_ref': total_ref,
    }


def compute_all_metrics(
    onset_probs: torch.Tensor,
    offset_probs: torch.Tensor,
    frame_probs: torch.Tensor,
    velocity_preds: torch.Tensor,
    onset_labels: torch.Tensor,
    offset_labels: torch.Tensor,
    frame_labels: torch.Tensor,
    velocity_labels: torch.Tensor,
    mask: torch.Tensor = None,
    threshold: float = 0.5,
    compute_note_metrics_: bool = True
) -> Dict[str, float]:
    """
    Compute all evaluation metrics and flatten into single dict.
    
    Args:
        onset_probs: Predicted onset probabilities (batch, time, 88)
        offset_probs: Predicted offset probabilities (batch, time, 88)
        frame_probs: Predicted frame probabilities (batch, time, 88)
        velocity_preds: Predicted velocities (batch, time, 88)
        onset_labels: Ground truth onsets (batch, time, 88)
        offset_labels: Ground truth offsets (batch, time, 88)
        frame_labels: Ground truth frames (batch, time, 88)
        velocity_labels: Ground truth velocities (batch, time, 88)
        mask: Optional mask for padding (batch, time)
        threshold: Classification threshold for binary predictions (default: 0.5)
        compute_note_metrics_: Whether to compute note-level metrics (default: True)
    
    Returns:
        Flat dictionary with all metrics:
            Frame-level: onset_precision, onset_recall, onset_f1, onset_accuracy,
                        offset_precision, offset_recall, offset_f1, offset_accuracy,
                        frame_precision, frame_recall, frame_f1, frame_accuracy,
                        velocity_mae
            Note-level: note_precision, note_recall, note_f1, num_notes_pred, num_notes_ref
                       (only if compute_note_metrics_ is True and decoder available)
    """
    metrics = {}
    
    # Compute frame-level metrics
    onset_metrics = compute_onset_metrics(onset_probs, onset_labels, threshold, mask)
    offset_metrics = compute_offset_metrics(offset_probs, offset_labels, threshold, mask)
    frame_metrics = compute_frame_metrics(frame_probs, frame_labels, threshold, mask)
    velocity_mae = compute_velocity_mae(velocity_preds, velocity_labels, onset_labels, mask)
    
    # Flatten into single dict
    metrics.update(onset_metrics)
    metrics.update(offset_metrics)
    metrics.update(frame_metrics)
    metrics['velocity_mae'] = velocity_mae
    
    # Compute note-level metrics (actual transcription quality)
    if compute_note_metrics_:
        try:
            note_metrics = compute_note_metrics(
                onset_probs, offset_probs, frame_probs, velocity_preds,
                onset_labels, offset_labels, frame_labels, velocity_labels,
                onset_thresh=threshold,
                offset_thresh=threshold,
                frame_thresh=threshold,
                mask=mask
            )
            metrics.update(note_metrics)
        except Exception as e:
            print(f"Warning: Could not compute note-level metrics: {e}")
    
    return metrics


def print_metrics(metrics: Dict[str, float]):
    """
    Pretty print evaluation metrics.
    
    Args:
        metrics: Output from compute_all_metrics() - flat dict with all metrics
    """
    print("\nEvaluation Metrics:")
    print("=" * 60)
    
    # Group metrics by type
    onset_metrics = {k: v for k, v in metrics.items() if k.startswith('onset_')}
    offset_metrics = {k: v for k, v in metrics.items() if k.startswith('offset_')}
    frame_metrics = {k: v for k, v in metrics.items() if k.startswith('frame_')}
    note_metrics = {k: v for k, v in metrics.items() if k.startswith('note_')}
    velocity_mae = metrics.get('velocity_mae', None)
    
    if onset_metrics:
        print("\nOnset Detection (Frame-Level):")
        for k, v in onset_metrics.items():
            print(f"  {k:20s}: {v:.4f}")
    
    if offset_metrics:
        print("\nOffset Detection (Frame-Level):")
        for k, v in offset_metrics.items():
            print(f"  {k:20s}: {v:.4f}")
    
    if frame_metrics:
        print("\nFrame Predictions (Frame-Level):")
        for k, v in frame_metrics.items():
            print(f"  {k:20s}: {v:.4f}")
    
    if velocity_mae is not None:
        print(f"\nVelocity MAE (Frame-Level): {velocity_mae:.4f}")
    
    # NOTE: Note-level metrics are most important for actual transcription quality!
    if note_metrics:
        print("\n" + "=" * 60)
        print("NOTE-LEVEL METRICS (Most Important for Transcription Quality):")
        print("=" * 60)
        
        # Extract note-level metrics for pretty printing
        note_f1 = note_metrics.get('note_f1', None)
        note_precision = note_metrics.get('note_precision', None)
        note_recall = note_metrics.get('note_recall', None)
        num_pred = note_metrics.get('num_notes_pred', None)
        num_ref = note_metrics.get('num_notes_ref', None)
        
        if note_f1 is not None:
            print(f"\n  Precision: {note_precision:.4f}")
            print(f"  Recall:    {note_recall:.4f}")
            print(f"  F1 Score:  {note_f1:.4f}")
        
        if num_pred is not None and num_ref is not None:
            print(f"\n  Predicted Notes: {int(num_pred):4d}")
            print(f"  Reference Notes: {int(num_ref):4d}")
    else:
        print("\n(Note-level metrics not available)")
    
    print("\n" + "=" * 60)
