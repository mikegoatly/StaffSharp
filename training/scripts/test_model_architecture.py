"""
Test model architecture (forward pass, output shapes, gradients).
"""

import torch
import pytest
from model import OnsetsAndFramesModel
from test_helpers import create_batch, device, model, criterion

class TestModelArchitecture:
    def test_model_creation(self, model):
        assert model is not None
        assert isinstance(model, OnsetsAndFramesModel)

    def test_forward_pass_output_shapes(self, model, device):
        batch_size = 4
        num_keys = 88
        sequence_length = 500
        num_mels = 229
        mel_specs = torch.randn(batch_size, sequence_length, num_mels, device=device)
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        expected_shape = (batch_size, sequence_length, num_keys)
        assert onset_pred.shape == expected_shape
        assert offset_pred.shape == expected_shape
        assert frame_pred.shape == expected_shape
        assert velocity_pred.shape == expected_shape

    def test_forward_pass_with_different_sequence_lengths(self, model, device):
        batch_size = 2
        num_mels = 229
        num_keys = 88
        for seq_len in [100, 500, 2000]:
            mel_specs = torch.randn(batch_size, seq_len, num_mels, device=device)
            onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
            expected_shape = (batch_size, seq_len, num_keys)
            assert onset_pred.shape == expected_shape
            assert offset_pred.shape == expected_shape
            assert frame_pred.shape == expected_shape
            assert velocity_pred.shape == expected_shape

    def test_model_gradient_flow(self, model, criterion, device):
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        loss, loss_dict = criterion(onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets, mask=mask)
        loss.backward()
        has_gradients = any(p.grad is not None and (p.grad != 0).any() for p in model.parameters())
        assert has_gradients, "No gradients computed"

    def test_model_invalid_input_shape(self, model, device):
        mel_specs_wrong = torch.randn(4, 500, 100, device=device)
        with pytest.raises((AssertionError, RuntimeError, ValueError)):
            model(mel_specs_wrong)
