"""
Test loss function computation and masking.
"""

import torch
import pytest
from test_helpers import create_batch, device, model, criterion
from model import OnsetsAndFramesLoss

class TestLossFunction:
    def test_loss_computation(self, model, criterion, device):
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        loss, loss_dict = criterion(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        assert loss is not None
        assert loss.item() > 0
        assert not torch.isnan(loss)
        assert not torch.isinf(loss)

    def test_loss_dict_keys(self, model, criterion, device):
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        loss, loss_dict = criterion(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        expected_keys = {'total', 'onset', 'offset', 'frame', 'velocity'}
        assert set(loss_dict.keys()) == expected_keys
        assert all(v > 0 for v in loss_dict.values())

    def test_loss_with_masking(self, model, criterion, device):
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(
            batch_size=2,
            device=device
        )
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        loss_full, _ = criterion(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        mask_half = mask.clone()
        mask_half[:, mask_half.shape[1]//2:] = 0
        loss_partial, _ = criterion(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask_half
        )
        assert isinstance(loss_full.item(), float)
        assert isinstance(loss_partial.item(), float)

    def test_configurable_loss_weights(self, device):
        criterion_default = OnsetsAndFramesLoss()
        criterion_high_onset = OnsetsAndFramesLoss(onset_weight=100.0)
        _, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
        batch_size, seq_len, num_keys = 4, 500, 88
        onset_pred = torch.randn(batch_size, seq_len, num_keys, device=device)
        offset_pred = torch.randn(batch_size, seq_len, num_keys, device=device)
        frame_pred = torch.randn(batch_size, seq_len, num_keys, device=device)
        velocity_pred = torch.randn(batch_size, seq_len, num_keys, device=device)
        loss_default, _ = criterion_default(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        loss_high, _ = criterion_high_onset(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        assert loss_high.item() > loss_default.item()
