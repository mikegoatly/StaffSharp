"""
Shared fixtures and helpers for training pipeline tests.
"""
import torch
import pytest
from model import create_model, OnsetsAndFramesLoss

@pytest.fixture
def device():
    return 'cuda' if torch.cuda.is_available() else 'cpu'

@pytest.fixture
def model(device):
    return create_model(
        device=device,
        num_mels=229,
        projection_size=256,
        num_layers=2,
        hidden_size=128
    )

@pytest.fixture
def criterion():
    return OnsetsAndFramesLoss(
        onset_weight=5.0,
        onset_pos_weight=50.0,
        offset_weight=5.0,
        frame_weight=2.0,
        velocity_weight=1.0
    )

def create_batch(
    batch_size: int = 4,
    num_mels: int = 229,
    sequence_length: int = 500,
    num_keys: int = 88,
    device: str = 'cpu'
):
    mel_specs = torch.randn(batch_size, sequence_length, num_mels, device=device)
    onset_targets = torch.randint(0, 2, (batch_size, sequence_length, num_keys), dtype=torch.float32, device=device)
    offset_targets = torch.randint(0, 2, (batch_size, sequence_length, num_keys), dtype=torch.float32, device=device)
    frame_targets = torch.randint(0, 2, (batch_size, sequence_length, num_keys), dtype=torch.float32, device=device)
    velocity_targets = torch.rand(batch_size, sequence_length, num_keys, device=device)
    mask = torch.ones(batch_size, sequence_length, device=device)
    return mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask
