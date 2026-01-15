"""
Create a minimal ONNX model for testing the OnnxPolyphonicTranscriber.

This script generates a simple (non-trained) ONNX model with the correct
input/output signature for the Onsets and Frames architecture. The model
uses trivial operations (identity, constant outputs) to enable testing
of the inference pipeline without requiring a trained model.

Usage:
    python create_test_model.py --output test_model.onnx
"""

import argparse
import numpy as np
import torch
import torch.nn as nn

from onnx_utils import export_to_onnx_format, verify_onnx_model


class DummyOnsetsFramesModel(nn.Module):
    """
    Minimal model that matches the Onsets and Frames signature.

    Input: (batch, time, mel_bins) - mel spectrogram features
    Outputs:
        - onset_probs: (batch, time, 88) - onset probabilities
        - offset_probs: (batch, time, 88) - offset probabilities
        - frame_probs: (batch, time, 88) - frame activation probabilities
        - velocities: (batch, time, 88) - normalized velocities [0-1]

    This dummy model uses simple linear projections to transform the input
    into the expected output shape with valid values (sigmoid/relu for proper ranges).
    """

    def __init__(self, mel_bins: int = 229, piano_keys: int = 88):
        super().__init__()
        self.mel_bins = mel_bins
        self.piano_keys = piano_keys

        # Simple linear projections to convert mel spectrogram to piano roll outputs
        # These are not trained, just initialized randomly for testing
        self.onset_projection = nn.Linear(mel_bins, piano_keys)
        self.offset_projection = nn.Linear(mel_bins, piano_keys)
        self.frame_projection = nn.Linear(mel_bins, piano_keys)
        self.velocity_projection = nn.Linear(mel_bins, piano_keys)

    def forward(self, mel_spectrogram):
        """
        Forward pass through dummy model.

        Args:
            mel_spectrogram: (batch, time, mel_bins) input tensor

        Returns:
            Tuple of (onset_probs, offset_probs, frame_probs, velocities)
        """
        # Project to piano key space
        # Onsets: sigmoid to get probabilities [0, 1]
        onset_probs = torch.sigmoid(self.onset_projection(mel_spectrogram))

        # Offsets: sigmoid to get probabilities [0, 1]
        offset_probs = torch.sigmoid(self.offset_projection(mel_spectrogram))

        # Frames: sigmoid to get probabilities [0, 1]
        frame_probs = torch.sigmoid(self.frame_projection(mel_spectrogram))

        # Velocities: sigmoid to get normalized values [0, 1]
        velocities = torch.sigmoid(self.velocity_projection(mel_spectrogram))

        return onset_probs, offset_probs, frame_probs, velocities


def create_test_model(output_path: str, mel_bins: int = 229, piano_keys: int = 88):
    """
    Create and export a test ONNX model.

    Args:
        output_path: Path to save the ONNX model
        mel_bins: Number of mel frequency bins (default: 229)
        piano_keys: Number of piano keys (default: 88)
    """
    print(f"Creating dummy Onsets and Frames model...")
    print(f"  Mel bins: {mel_bins}")
    print(f"  Piano keys: {piano_keys}")

    # Create model instance
    model = DummyOnsetsFramesModel(mel_bins, piano_keys)
    model.eval()  # Set to evaluation mode

    # Create dummy input (batch=1, time=10 frames, mel_bins)
    # Using dynamic time axis so the model accepts any sequence length
    dummy_input = torch.randn(1, 10, mel_bins)

    # Define input/output names
    input_names = ['input']
    output_names = ['onset_probs', 'offset_probs', 'frame_probs', 'velocities']

    # Define dynamic axes (batch and time can vary)
    dynamic_axes = {
        'input': {0: 'batch', 1: 'time'},
        'onset_probs': {0: 'batch', 1: 'time'},
        'offset_probs': {0: 'batch', 1: 'time'},
        'frame_probs': {0: 'batch', 1: 'time'},
        'velocities': {0: 'batch', 1: 'time'}
    }

    print(f"\nExporting to ONNX format...")
    print(f"  Input: {input_names[0]} with shape (batch, time, {mel_bins})")
    print(f"  Outputs:")
    for name in output_names:
        print(f"    - {name} with shape (batch, time, {piano_keys})")

    # Export to ONNX using shared utility
    export_to_onnx_format(
        model=model,
        dummy_input=dummy_input,
        output_path=output_path,
        input_names=input_names,
        output_names=output_names,
        opset_version=18,
        verbose=False
    )

    print(f"\nModel saved to: {output_path}")

    # Verify the exported model using shared utility
    verify_onnx_model(output_path, mel_bins=mel_bins, piano_keys=piano_keys)


def main():
    parser = argparse.ArgumentParser(
        description="Create a test ONNX model for Onsets and Frames transcription"
    )
    parser.add_argument(
        "--output",
        type=str,
        default="test_onsets_frames.onnx",
        help="Output path for the ONNX model (default: test_onsets_frames.onnx)"
    )
    parser.add_argument(
        "--mel-bins",
        type=int,
        default=229,
        help="Number of mel frequency bins (default: 229)"
    )
    parser.add_argument(
        "--piano-keys",
        type=int,
        default=88,
        help="Number of piano keys (default: 88)"
    )

    args = parser.parse_args()

    create_test_model(args.output, args.mel_bins, args.piano_keys)

    print("\n" + "="*60)
    print("Test model created successfully!")
    print("="*60)
    print("\nYou can now use this model to test the C# OnnxPolyphonicTranscriber:")
    print(f"  Model path: {args.output}")
    print("\nNote: This is a dummy model with random weights.")
    print("For actual transcription, you'll need to train a real model.")


if __name__ == "__main__":
    main()
