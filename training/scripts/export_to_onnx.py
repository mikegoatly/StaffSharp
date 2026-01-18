"""
Export trained PyTorch model to ONNX format for inference in C# via ML.NET.

Usage:
    python export_to_onnx.py --checkpoint models/best_model.pt --output models/onsets_frames.onnx
"""

import argparse
from pathlib import Path

import torch

from model import create_model
from onnx_utils import export_to_onnx_format, verify_onnx_model, quantize_onnx_model


def export_to_onnx(checkpoint_path: str, output_path: str, opset_version: int = 18, quantize: str = None):
    """
    Export trained model to ONNX format.

    Args:
        checkpoint_path: Path to PyTorch checkpoint (.pt file)
        output_path: Output path for ONNX model (.onnx file)
        opset_version: ONNX opset version (default: 18, compatible with ONNX Runtime 1.15+)
        quantize: Quantization mode: 'float16', 'dynamic', or None (default: None)
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
    print(f"  Output shapes: (batch={batch_size}, time=dynamic, keys=88) x4")

    # Export to ONNX using shared utility
    export_to_onnx_format(
        model=model,
        dummy_input=dummy_input,
        output_path=output_path,
        input_names=['input'],
        output_names=['onset_probs', 'offset_probs', 'frame_probs', 'velocities'],
        opset_version=opset_version,
        verbose=False
    )

    print(f"\nModel exported successfully to: {output_path}")

    # Verify exported model using shared utility
    verify_onnx_model(output_path)

    # Test inference with ONNX Runtime
    import onnx
    import onnxruntime as ort
    session = ort.InferenceSession(output_path)

    # Test with different sequence lengths
    test_lengths = [50, 100, 200]
    for length in test_lengths:
        test_input = torch.randn(1, length, mel_bins).numpy()
        outputs = session.run(
            ['onset_probs', 'offset_probs', 'frame_probs', 'velocities'],
            {'input': test_input}
        )

        print(f"  Inference test passed for length={length}")
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

    # Apply quantization if requested
    if quantize:
        print(f"\n{'='*60}")
        print("Applying quantization...")
        print(f"{'='*60}")

        # Generate quantized output path
        output_stem = Path(output_path).stem
        output_dir = Path(output_path).parent
        quantized_path = output_dir / f"{output_stem}_{quantize}.onnx"

        try:
            quantize_onnx_model(
                input_path=output_path,
                output_path=str(quantized_path),
                quantization_mode=quantize
            )

            print(f"\nQuantized model saved to: {quantized_path}")

            # Verify quantized model
            print(f"\nVerifying quantized model...")
            verify_onnx_model(str(quantized_path))

            # Test inference with quantized model
            import onnxruntime as ort
            session = ort.InferenceSession(str(quantized_path))

            print(f"\nTesting quantized model inference:")
            for length in [50, 100, 200]:
                test_input = torch.randn(1, length, mel_bins).numpy()
                outputs = session.run(
                    ['onset_probs', 'offset_probs', 'frame_probs', 'velocities'],
                    {'input': test_input}
                )
                print(f"  Inference test passed for length={length}")

            print(f"\nQuantization complete!")
            print(f"  Original:  {output_path} ({Path(output_path).stat().st_size / 1024 / 1024:.2f} MB)")
            print(f"  Quantized: {quantized_path} ({Path(quantized_path).stat().st_size / 1024 / 1024:.2f} MB)")

        except Exception as e:
            print(f"\nWARNING: Quantization failed: {e}")
            print(f"  Original model is still available at: {output_path}")

    print(f"\nExport complete!")


def main():
    parser = argparse.ArgumentParser(
        description='Export model to ONNX',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog='''
Quantization options:
  --quantize float16   ~50%% size reduction, minimal accuracy loss (recommended)
  --quantize dynamic   ~75%% size reduction, some accuracy loss
        '''
    )
    parser.add_argument('--checkpoint', type=str, required=True,
                        help='Path to PyTorch checkpoint (.pt file)')
    parser.add_argument('--output', type=str, required=True,
                        help='Output path for ONNX model (.onnx file)')
    parser.add_argument('--opset-version', type=int, default=18,
                        help='ONNX opset version (default: 18)')
    parser.add_argument('--quantize', type=str, choices=['float16', 'dynamic'], default=None,
                        help='Quantization mode to reduce model size')

    args = parser.parse_args()

    # Create output directory if needed
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)

    # Export
    export_to_onnx(args.checkpoint, args.output, args.opset_version, args.quantize)


if __name__ == '__main__':
    main()
