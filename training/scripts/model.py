"""
Onsets and Frames model architecture for polyphonic piano transcription.

Based on the paper:
"Onsets and Frames: Dual-Objective Piano Transcription"
by Curtis Hawthorne et al. (2018)
https://arxiv.org/abs/1710.11153

Architecture:
    Input: Mel spectrogram (time_frames, 229 mel bins)
    ↓
    Acoustic Model (CNN + BiLSTM)
    ↓
    Three prediction heads:
        - Onset probabilities (88 piano keys)
        - Frame activations (88 piano keys)
        - Velocities (88 piano keys)
"""

import torch
import torch.nn as nn
import torch.nn.functional as F

class OnsetsAndFramesModel(nn.Module):
    def __init__(
        self,
        input_features: int = 229,
        output_features: int = 88,
        # Standard O&F typically uses 3 blocks of convs
        model_complexity: int = 48, 
        lstm_hidden_size: int = 256,
        lstm_num_layers: int = 3, # Paper usually uses 3 layers
        dropout: float = 0.5
    ):
        super().__init__()
        
        # --- Acoustic Model (CNN) ---
        # The paper uses 3 blocks. Each block: Conv -> Conv -> MaxPool(freq)
        # Using a simpler stack that reduces frequency dim is crucial.
        
        self.cnn_layers = nn.Sequential(
            # Block 1
            nn.Conv2d(1, model_complexity, kernel_size=3, padding=1),
            nn.BatchNorm2d(model_complexity),
            nn.ReLU(),
            nn.Conv2d(model_complexity, model_complexity, kernel_size=3, padding=1),
            nn.BatchNorm2d(model_complexity),
            nn.ReLU(),
            nn.MaxPool2d(kernel_size=(1, 2), stride=(1, 2)), # Pool freq only
            nn.Dropout(0.25),

            # Block 2
            nn.Conv2d(model_complexity, model_complexity * 2, kernel_size=3, padding=1),
            nn.BatchNorm2d(model_complexity * 2),
            nn.ReLU(),
            nn.MaxPool2d(kernel_size=(1, 2), stride=(1, 2)), # Pool freq only
            nn.Dropout(0.25),

            # Block 3
            nn.Conv2d(model_complexity * 2, model_complexity * 4, kernel_size=3, padding=1),
            nn.BatchNorm2d(model_complexity * 4),
            nn.ReLU(),
            nn.MaxPool2d(kernel_size=(1, 2), stride=(1, 2)), # Pool freq only
            nn.Dropout(0.25),
        )
        
        # Calculate output size after 3 poolings of stride 2:
        # 229 -> 114 -> 57 -> 28
        cnn_out_freq = input_features // 8 
        cnn_out_channels = model_complexity * 4
        self.cnn_output_size = cnn_out_channels * cnn_out_freq
        
        # Projection
        self.fc_projection = nn.Linear(self.cnn_output_size, 768)

        # --- Recurrent Model ---
        self.lstm = nn.LSTM(
            input_size=768,
            hidden_size=lstm_hidden_size,
            num_layers=lstm_num_layers,
            batch_first=True,
            bidirectional=True,
            dropout=dropout
        )
        
        # --- Heads ---
        lstm_out = lstm_hidden_size * 2
        
        self.onset_head = nn.Linear(lstm_out, output_features)
        self.offset_head = nn.Linear(lstm_out, output_features) # Added Offset
        self.velocity_head = nn.Linear(lstm_out, output_features)
        
        # Frame head inputs: LSTM + Onset + Offset
        self.frame_head = nn.Linear(lstm_out + output_features * 2, output_features)

    def forward(self, mel_spec: torch.Tensor):
        # mel_spec: (batch, time, mel_bins)
        
        # 1. CNN
        x = mel_spec.unsqueeze(1) # (B, 1, T, F)
        x = self.cnn_layers(x)    # (B, C, T, F_reduced)
        
        # Prepare for Linear: (B, T, C * F_reduced)
        x = x.permute(0, 2, 1, 3) 
        x = x.flatten(2) 
        
        # 2. Projection
        x = self.fc_projection(x)
        x = F.relu(x)
        
        # 3. LSTM
        x, _ = self.lstm(x) # (B, T, hidden*2)
        
        # 4. Heads
        onsets = self.onset_head(x)
        offsets = self.offset_head(x)
        velocities = self.velocity_head(x)
        
        # 5. Frame Head (Autoregressive)
        # CRITICAL: Do NOT detach here. Let gradients flow back to onset/offset heads.
        onset_probs = torch.sigmoid(onsets)
        offset_probs = torch.sigmoid(offsets)
        
        frame_input = torch.cat([x, onset_probs, offset_probs], dim=-1)
        frames = self.frame_head(frame_input)
        
        return onsets, offsets, frames, velocities

    def predict(self, mel_spec: torch.Tensor):
        """
        Prediction mode with no gradient computation.

        Args:
            mel_spec: (batch, time, mel_bins) or (time, mel_bins)

        Returns:
            onset_probs: (batch, time, 88) or (time, 88)
            offset_probs: (batch, time, 88) or (time, 88)
            frame_probs: (batch, time, 88) or (time, 88)
            velocities: (batch, time, 88) or (time, 88)
        """
        self.eval()
        with torch.no_grad():
            scalar_input = (mel_spec.dim() == 2)
            if scalar_input:
                mel_spec = mel_spec.unsqueeze(0)
            
            # Forward pass
            onsets, offsets, frames, velocities = self.forward(mel_spec)
                
            onsets = torch.sigmoid(onsets)
            offsets = torch.sigmoid(offsets)
            frames = torch.sigmoid(frames)
            velocities = torch.sigmoid(velocities)
            
            if scalar_input:
                return onsets.squeeze(0), offsets.squeeze(0), frames.squeeze(0), velocities.squeeze(0)
            else:
                return onsets, offsets, frames, velocities


class OnsetsAndFramesLoss(nn.Module):
    """
    Combined loss function for onset, offset, frame, and velocity prediction.
    """

    def __init__(self, onset_weight=1.0, offset_weight=1.0, frame_weight=1.0, velocity_weight=1.0):
        super().__init__()
        self.onset_weight = onset_weight
        self.offset_weight = offset_weight
        self.frame_weight = frame_weight
        self.velocity_weight = velocity_weight
        
        # Standard BCE With Logits but with positive weights to handle class imbalance
        # Notes are rare events in the time-frequency space
        self.onset_bce = nn.BCEWithLogitsLoss(pos_weight=torch.tensor([5.0]))
        self.offset_bce = nn.BCEWithLogitsLoss(pos_weight=torch.tensor([5.0]))
        self.frame_bce = nn.BCEWithLogitsLoss(pos_weight=torch.tensor([2.0]))
        
        self.velocity_criterion = nn.MSELoss(reduction='none')

    def forward(self, onsets, offsets, frames, velocities, 
                onset_label, offset_label, frame_label, velocity_label, mask=None):
        
        # Ensure weights are on the correct device
        if self.onset_bce.pos_weight.device != onsets.device:
            self.onset_bce.pos_weight = self.onset_bce.pos_weight.to(onsets.device)
            self.offset_bce.pos_weight = self.offset_bce.pos_weight.to(onsets.device)
            self.frame_bce.pos_weight = self.frame_bce.pos_weight.to(onsets.device)

        # Expand mask to (B, T, 88) if provided
        if mask is not None:
            mask_expanded = mask.unsqueeze(-1).expand_as(onsets)
        else:
            mask_expanded = torch.ones_like(onsets)

        # 1. Classification Losses with masking
        # Use reduction='none' to get per-element loss, then apply mask
        onset_loss_raw = torch.nn.functional.binary_cross_entropy_with_logits(
            onsets, onset_label, pos_weight=self.onset_bce.pos_weight, reduction='none'
        )
        onset_loss = (onset_loss_raw * mask_expanded).sum() / mask_expanded.sum()
        
        offset_loss_raw = torch.nn.functional.binary_cross_entropy_with_logits(
            offsets, offset_label, pos_weight=self.offset_bce.pos_weight, reduction='none'
        )
        offset_loss = (offset_loss_raw * mask_expanded).sum() / mask_expanded.sum()
        
        frame_loss_raw = torch.nn.functional.binary_cross_entropy_with_logits(
            frames, frame_label, pos_weight=self.frame_bce.pos_weight, reduction='none'
        )
        frame_loss = (frame_loss_raw * mask_expanded).sum() / mask_expanded.sum()
        
        # 2. Velocity Loss (Masked by both onset and padding mask)
        # Only compute velocity loss where a note actually starts (onset_label == 1)
        vel_pred = torch.sigmoid(velocities)
        velocity_loss_raw = self.velocity_criterion(vel_pred, velocity_label)
        
        onset_mask = (onset_label == 1) & (mask_expanded == 1)
        if onset_mask.sum() > 0:
            velocity_loss = (velocity_loss_raw * onset_mask).sum() / onset_mask.sum()
        else:
            velocity_loss = torch.tensor(0.0, device=onsets.device)

        # 3. Total
        total_loss = (
            self.onset_weight * onset_loss +
            self.offset_weight * offset_loss +
            self.frame_weight * frame_loss +
            self.velocity_weight * velocity_loss
        )
        
        loss_dict = {
            'total': total_loss.item(),
            'onset': onset_loss.item(),
            'offset': offset_loss.item(),
            'frame': frame_loss.item(),
            'velocity': velocity_loss.item()
        }
        
        return total_loss, loss_dict


def create_model(device: str = 'cuda') -> OnsetsAndFramesModel:
    """
    Create a default Onsets and Frames model.

    Args:
        device: Device to place model on ('cuda' or 'cpu')

    Returns:
        model: Initialized model on specified device
    """
    model = OnsetsAndFramesModel(
        input_features=229,
        output_features=88,
        model_complexity=48,
        lstm_hidden_size=256,
        lstm_num_layers=3,
        dropout=0.5
    )

    return model.to(device)


if __name__ == '__main__':
    # Test model
    print("Testing Onsets and Frames model...")

    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    print(f"Using device: {device}")

    # Create model
    model = create_model(device)

    # Test forward pass
    batch_size = 2
    time_steps = 100
    mel_bins = 229

    x = torch.randn(batch_size, time_steps, mel_bins).to(device)
    onsets, offsets, frames, velocities = model(x)

    print(f"\nInput shape: {x.shape}")
    print(f"Onset output shape: {onsets.shape}")
    print(f"Offset output shape: {offsets.shape}")
    print(f"Frame output shape: {frames.shape}")
    print(f"Velocity output shape: {velocities.shape}")

    # Test loss
    loss_fn = OnsetsAndFramesLoss()
    onset_target = torch.randint(0, 2, (batch_size, time_steps, 88)).float().to(device)
    offset_target = torch.randint(0, 2, (batch_size, time_steps, 88)).float().to(device)
    frame_target = torch.randint(0, 2, (batch_size, time_steps, 88)).float().to(device)
    velocity_target = torch.rand(batch_size, time_steps, 88).to(device)

    total_loss, loss_dict = loss_fn(
        onsets, offsets, frames, velocities, 
        onset_target, offset_target, frame_target, velocity_target
    )

    print(f"\nLoss computation:")
    print(f"  Total: {loss_dict['total']:.4f}")
    print(f"  Onset: {loss_dict['onset']:.4f}")
    print(f"  Offset: {loss_dict['offset']:.4f}")
    print(f"  Frame: {loss_dict['frame']:.4f}")
    print(f"  Velocity: {loss_dict['velocity']:.4f}")

    # Count parameters
    total_params = sum(p.numel() for p in model.parameters())
    trainable_params = sum(p.numel() for p in model.parameters() if p.requires_grad)

    print(f"\nModel parameters:")
    print(f"  Total: {total_params:,}")
    print(f"  Trainable: {trainable_params:,}")

    print("\n✓ Model test passed!")
