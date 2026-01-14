"""
Export trained PyTorch model to ONNX format for inference in C# via ML.NET.

Usage:
    python export_to_onnx.py --checkpoint models/best_model.pt --output models/onsets_frames.onnx
"""

import argparse
from pathlib import Path

import torch
import torch.onnx

from model import create_model


def export_to_onnx(checkpoint_path: str, output_path: str, opset_version: int = 17):
    """
    Export trained model to ONNX format.

    Args:
        checkpoint_path: Path to PyTorch checkpoint (.pt file)
        output_path: Output path for ONNX model (.onnx file)
        opset_version: ONNX opset version (default: 17, compatible with ONNX Runtime 1.15+)
    """
    print(f"Loading checkpoint from: {checkpoint_path}")

    # Load checkpoint
    checkpoint = torch.load(checkpoint_path, map_location='cpu')

    # Create model
    model = create_model('cpu')
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()

    print(f"Loaded model from epoch {checkpoint.get('epoch', 'unknown')}")
    if 'metrics' in checkpoint:
        print(f"Model metrics: {checkpoint['metrics']}")

    # Create dummy input
    batch_size = 1
    time_steps = 100  # Arbitrary, will support dynamic length
    mel_bins = 229

    dummy_input = torch.randn(batch_size, time_steps, mel_bins)

    print(f"\nExporting model to ONNX...")
    print(f"  Input shape: (batch={batch_size}, time=dynamic, mel_bins={mel_bins})")
    print(f"  Output shapes: (batch={batch_size}, time=dynamic, keys=88) x3")

    # Export to ONNX
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        export_params=True,
        opset_version=opset_version,
        do_constant_folding=True,
        input_names=['input'],
        output_names=['onset_probs', 'frame_probs', 'velocities'],
        dynamic_axes={
            'input': {0: 'batch_size', 1: 'time'},
            'onset_probs': {0: 'batch_size', 1: 'time'},
            'frame_probs': {0: 'batch_size', 1: 'time'},
            'velocities': {0: 'batch_size', 1: 'time'}
        },
        verbose=False
    )

    print(f"\n✓ Model exported successfully to: {output_path}")

    # Verify exported model
    print("\nVerifying exported model...")
    import onnx
    import onnxruntime as ort

    # Load and check ONNX model
    onnx_model = onnx.load(output_path)
    onnx.checker.check_model(onnx_model)
    print("  ✓ ONNX model is valid")

    # Test inference with ONNX Runtime
    session = ort.InferenceSession(output_path)

    # Test with different sequence lengths
    test_lengths = [50, 100, 200]
    for length in test_lengths:
        test_input = torch.randn(1, length, mel_bins).numpy()
        outputs = session.run(
            ['onset_probs', 'frame_probs', 'velocities'],
            {'input': test_input}
        )

        print(f"  ✓ Inference test passed for length={length}")
        print(f"    Output shapes: {[o.shape for o in outputs]}")

    # Print model info
    print(f"\nModel information:")
    print(f"  Opset version: {opset_version}")
    print(f"  File size: {Path(output_path).stat().st_size / 1024 / 1024:.2f} MB")

    # Print inputs/outputs
    print(f"\n  Inputs:")
    for inp in session.get_inputs():
        print(f"    - {inp.name}: {inp.shape} ({inp.type})")

    print(f"\n  Outputs:")
    for out in session.get_outputs():
        print(f"    - {out.name}: {out.shape} ({out.type})")

    print(f"\n✓ Export complete!")
    print(f"\nNext steps:")
    print(f"  1. Copy ONNX model to C# project:")
    print(f"     cp {output_path} ../test/StaffSharp.MachineLearning.Tests/TestData/")
    print(f"  2. Run C# tests to verify integration:")
    print(f"     dotnet test StaffSharp.MachineLearning.Tests")


def main():
    parser = argparse.ArgumentParser(description='Export model to ONNX')
    parser.add_argument('--checkpoint', type=str, required=True,
                        help='Path to PyTorch checkpoint (.pt file)')
    parser.add_argument('--output', type=str, required=True,
                        help='Output path for ONNX model (.onnx file)')
    parser.add_argument('--opset-version', type=int, default=17,
                        help='ONNX opset version (default: 17)')

    args = parser.parse_args()

    # Create output directory if needed
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)

    # Export
    export_to_onnx(args.checkpoint, args.output, args.opset_version)


if __name__ == '__main__':
    main()
