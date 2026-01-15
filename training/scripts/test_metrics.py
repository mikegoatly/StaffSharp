"""
Test evaluation metrics computation.
"""
import torch
import numpy as np
from metrics import (
    compute_onset_metrics, compute_frame_metrics, compute_offset_metrics,
    compute_velocity_mae, compute_all_metrics
)

class TestMetrics:
    def test_onset_metrics_perfect_prediction(self):
        batch_size, num_keys, seq_len = 4, 88, 100
        onset_probs = torch.ones(batch_size, num_keys, seq_len)
        onset_labels = torch.ones(batch_size, num_keys, seq_len)
        metrics = compute_onset_metrics(onset_probs, onset_labels, threshold=0.5)
        assert metrics['onset_precision'] == 1.0
        assert metrics['onset_recall'] == 1.0
        assert metrics['onset_f1'] == 1.0
        assert metrics['onset_accuracy'] == 1.0

    def test_onset_metrics_worst_prediction(self):
        batch_size, num_keys, seq_len = 4, 88, 100
        onset_probs = torch.zeros(batch_size, num_keys, seq_len)
        onset_labels = torch.ones(batch_size, num_keys, seq_len)
        metrics = compute_onset_metrics(onset_probs, onset_labels, threshold=0.5)
        assert metrics['onset_recall'] == 0.0
        assert metrics['onset_f1'] == 0.0

    def test_frame_metrics(self):
        batch_size, num_keys, seq_len = 4, 88, 100
        frame_probs = torch.rand(batch_size, num_keys, seq_len)
        frame_labels = torch.randint(0, 2, (batch_size, num_keys, seq_len), dtype=torch.float32)
        metrics = compute_frame_metrics(frame_probs, frame_labels, threshold=0.5)
        assert 'frame_f1' in metrics
        assert 'frame_precision' in metrics
        assert 'frame_recall' in metrics
        assert all(0 <= v <= 1 for v in metrics.values())

    def test_velocity_mae(self):
        batch_size, num_keys, seq_len = 4, 88, 100
        velocity_preds = torch.ones(batch_size, num_keys, seq_len)
        velocity_labels = torch.ones(batch_size, num_keys, seq_len)
        mae = compute_velocity_mae(velocity_preds, velocity_labels)
        assert mae == 0.0

    def test_velocity_mae_with_error(self):
        batch_size, num_keys, seq_len = 4, 88, 100
        velocity_preds = torch.zeros(batch_size, num_keys, seq_len)
        velocity_labels = torch.ones(batch_size, num_keys, seq_len)
        mae = compute_velocity_mae(velocity_preds, velocity_labels)
        assert mae == 1.0

    def test_compute_all_metrics(self):
        device = 'cuda' if torch.cuda.is_available() else 'cpu'
        batch_size, seq_len, num_keys = 4, 100, 88
        onset_probs = torch.sigmoid(torch.randn(batch_size, seq_len, num_keys, device=device))
        offset_probs = torch.sigmoid(torch.randn(batch_size, seq_len, num_keys, device=device))
        frame_probs = torch.sigmoid(torch.randn(batch_size, seq_len, num_keys, device=device))
        velocity_preds = torch.sigmoid(torch.randn(batch_size, seq_len, num_keys, device=device))
        onset_labels = torch.randint(0, 2, (batch_size, seq_len, num_keys), dtype=torch.float32, device=device)
        offset_labels = torch.randint(0, 2, (batch_size, seq_len, num_keys), dtype=torch.float32, device=device)
        frame_labels = torch.randint(0, 2, (batch_size, seq_len, num_keys), dtype=torch.float32, device=device)
        velocity_labels = torch.rand(batch_size, seq_len, num_keys, device=device)
        mask = torch.ones(batch_size, seq_len, device=device)
        all_metrics = compute_all_metrics(
            onset_probs, offset_probs, frame_probs, velocity_preds,
            onset_labels, offset_labels, frame_labels, velocity_labels,
            mask=mask
        )
        expected_keys = {
            'onset_precision', 'onset_recall', 'onset_f1', 'onset_accuracy',
            'offset_precision', 'offset_recall', 'offset_f1', 'offset_accuracy',
            'frame_precision', 'frame_recall', 'frame_f1', 'frame_accuracy',
            'velocity_mae',
            'note_precision', 'note_recall', 'note_f1',
            'num_notes_pred', 'num_notes_ref'
        }
        assert set(all_metrics.keys()) == expected_keys
        for value in all_metrics.values():
            assert isinstance(value, (int, float))
            assert not np.isnan(value)
            assert not np.isinf(value)
