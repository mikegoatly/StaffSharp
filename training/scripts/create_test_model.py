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


class DummyOnsetsFramesModel(nn.Module):
    """
    Minimal model that matches the Onsets and Frames signature.

    Input: (batch, time, mel_bins) - mel spectrogram features
    Outputs:
        - onset_probs: (batch, time, 88) - onset probabilities
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
        self.frame_projection = nn.Linear(mel_bins, piano_keys)
        self.velocity_projection = nn.Linear(mel_bins, piano_keys)

    def forward(self, mel_spectrogram):
        """
        Forward pass through dummy model.

        Args:
            mel_spectrogram: (batch, time, mel_bins) input tensor

        Returns:
            Tuple of (onset_probs, frame_probs, velocities)
        """
        # Project to piano key space
        # Onsets: sigmoid to get probabilities [0, 1]
        onset_probs = torch.sigmoid(self.onset_projection(mel_spectrogram))

        # Frames: sigmoid to get probabilities [0, 1]
        frame_probs = torch.sigmoid(self.frame_projection(mel_spectrogram))

        # Velocities: sigmoid to get normalized values [0, 1]
        velocities = torch.sigmoid(self.velocity_projection(mel_spectrogram))

        return onset_probs, frame_probs, velocities


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
    output_names = ['onset_probs', 'frame_probs', 'velocities']

    # Define dynamic axes (batch and time can vary)
    dynamic_axes = {
        'input': {0: 'batch', 1: 'time'},
        'onset_probs': {0: 'batch', 1: 'time'},
        'frame_probs': {0: 'batch', 1: 'time'},
        'velocities': {0: 'batch', 1: 'time'}
    }

    print(f"\nExporting to ONNX format...")
    print(f"  Input: {input_names[0]} with shape (batch, time, {mel_bins})")
    print(f"  Outputs:")
    for name in output_names:
        print(f"    - {name} with shape (batch, time, {piano_keys})")

    # Export to ONNX
    import os
    # Suppress verbose output to avoid Unicode issues on Windows
    os.environ['PYTHONIOENCODING'] = 'utf-8'
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        input_names=input_names,
        output_names=output_names,
        dynamic_axes=dynamic_axes,
        opset_version=18,  # Use opset 18 to avoid conversion warnings
        do_constant_folding=True,
        export_params=True,
        verbose=False  # Suppress verbose output
    )

    print(f"\nModel saved to: {output_path}")

    # Verify the exported model
    verify_model(output_path)


def verify_model(model_path: str):
    """Verify the exported ONNX model can be loaded and run."""
    try:
        import onnx
        import onnxruntime as ort

        print(f"\nVerifying ONNX model...")

        # Load and check the model
        model = onnx.load(model_path)
        onnx.checker.check_model(model)
        print("  Model structure is valid")

        # Test inference with ONNX Runtime
        session = ort.InferenceSession(model_path)

        # Check inputs
        print(f"\n  Inputs:")
        for input_meta in session.get_inputs():
            print(f"    - {input_meta.name}: {input_meta.shape} ({input_meta.type})")

        # Check outputs
        print(f"  Outputs:")
        for output_meta in session.get_outputs():
            print(f"    - {output_meta.name}: {output_meta.shape} ({output_meta.type})")

        # Run a test inference
        dummy_input = np.random.randn(1, 50, 229).astype(np.float32)
        input_name = session.get_inputs()[0].name
        outputs = session.run(None, {input_name: dummy_input})

        print(f"\n  Test inference with input shape {dummy_input.shape}:")
        for i, (output_meta, output_data) in enumerate(zip(session.get_outputs(), outputs)):
            print(f"    - {output_meta.name}: shape={output_data.shape}, " +
                  f"range=[{output_data.min():.3f}, {output_data.max():.3f}]")

        print("\nModel verification successful!")

    except ImportError as e:
        print(f"\nVerification skipped: {e}")
        print("  Install onnx and onnxruntime to verify: pip install onnx onnxruntime")
    except Exception as e:
        print(f"\nVerification failed: {e}")
        raise


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
