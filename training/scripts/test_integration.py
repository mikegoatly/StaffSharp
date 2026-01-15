"""
Integration tests for full pipeline.
"""
import torch
from test_helpers import create_batch
from model import create_model, OnsetsAndFramesLoss
from metrics import compute_all_metrics

class TestIntegration:
    def test_full_training_step(self):
        device = 'cuda' if torch.cuda.is_available() else 'cpu'
        model = create_model(device=device, num_mels=229, projection_size=256, num_layers=2, hidden_size=128)
        criterion = OnsetsAndFramesLoss()
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
        onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
        loss, loss_dict = criterion(
            onset_pred, offset_pred, frame_pred, velocity_pred,
            onset_targets, offset_targets, frame_targets, velocity_targets,
            mask=mask
        )
        optimizer = torch.optim.Adam(model.parameters(), lr=0.001)
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        assert any(p.grad is not None for p in model.parameters())

    def test_full_validation_step(self):
        device = 'cuda' if torch.cuda.is_available() else 'cpu'
        model = create_model(device=device, num_mels=229, projection_size=256, num_layers=2, hidden_size=128)
        criterion = OnsetsAndFramesLoss()
        model.eval()
        with torch.no_grad():
            mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch(device=device)
            onset_pred, offset_pred, frame_pred, velocity_pred = model(mel_specs)
            loss, loss_dict = criterion(
                onset_pred, offset_pred, frame_pred, velocity_pred,
                onset_targets, offset_targets, frame_targets, velocity_targets,
                mask=mask
            )
            all_metrics = compute_all_metrics(
                torch.sigmoid(onset_pred), torch.sigmoid(offset_pred),
                torch.sigmoid(frame_pred), torch.sigmoid(velocity_pred),
                onset_targets, offset_targets, frame_targets, velocity_targets,
                mask=mask
            )
            assert all_metrics is not None
            assert len(all_metrics) > 0
