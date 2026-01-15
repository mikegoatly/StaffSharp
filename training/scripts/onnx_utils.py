"""
Shared utilities for ONNX model export and verification.

This module provides common functionality used by both export_to_onnx.py
and create_test_model.py to avoid code duplication.
"""

from pathlib import Path
from typing import Tuple, List

import numpy as np
import torch


def export_to_onnx_format(
    model: torch.nn.Module,
    dummy_input: torch.Tensor,
    output_path: str,
    input_names: List[str] = None,
    output_names: List[str] = None,
    opset_version: int = 17,
    verbose: bool = False
) -> None:
    """
    Export a PyTorch model to ONNX format with dynamic axes support.
    
    Args:
        model: PyTorch model to export (must be in eval mode)
        dummy_input: Example input tensor for ONNX tracing
        output_path: Path to save the ONNX model (.onnx file)
        input_names: List of input tensor names (default: ['input'])
        output_names: List of output tensor names 
                     (default: ['onset_probs', 'offset_probs', 'frame_probs', 'velocities'])
        opset_version: ONNX opset version (default: 17)
        verbose: Whether to print verbose export information (default: False)
    
    Raises:
        ValueError: If output_path is not a .onnx file
    """
    if not str(output_path).endswith('.onnx'):
        raise ValueError(f"Output path must be .onnx file, got: {output_path}")
    
    # Set defaults
    if input_names is None:
        input_names = ['input']
    if output_names is None:
        output_names = ['onset_probs', 'offset_probs', 'frame_probs', 'velocities']
    
    # Define dynamic axes for variable sequence lengths
    dynamic_axes = {
        'input': {0: 'batch', 1: 'time'},
        'onset_probs': {0: 'batch', 1: 'time'},
        'offset_probs': {0: 'batch', 1: 'time'},
        'frame_probs': {0: 'batch', 1: 'time'},
        'velocities': {0: 'batch', 1: 'time'}
    }
    
    # Create output directory if needed
    Path(output_path).parent.mkdir(parents=True, exist_ok=True)
    
    # Export to ONNX
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        input_names=input_names,
        output_names=output_names,
        dynamic_axes=dynamic_axes,
        opset_version=opset_version,
        do_constant_folding=True,
        export_params=True,
        verbose=verbose
    )


def verify_onnx_model(
    model_path: str,
    test_input_shapes: List[Tuple[int, int, int]] = None,
    mel_bins: int = 229,
    piano_keys: int = 88
) -> bool:
    """
    Verify that an exported ONNX model is valid and can run inference.
    
    Args:
        model_path: Path to ONNX model file
        test_input_shapes: List of input shapes to test
                          (default: [(1, 50, 229), (1, 100, 229), (1, 200, 229)])
        mel_bins: Expected number of mel frequency bins (default: 229)
        piano_keys: Expected number of piano keys (default: 88)
    
    Returns:
        True if model is valid and passes verification tests
    
    Raises:
        ImportError: If onnx or onnxruntime not available
        RuntimeError: If model verification fails
    """
    try:
        import onnx
        import onnxruntime as ort
    except ImportError as e:
        print(f"Verification skipped: {e}")
        print("Install onnx and onnxruntime to verify: pip install onnx onnxruntime")
        return False
    
    print(f"\nVerifying ONNX model: {model_path}")
    
    # Load and check model structure
    try:
        model = onnx.load(model_path)
        onnx.checker.check_model(model)
        print("  ✓ Model structure is valid")
    except Exception as e:
        print(f"  ✗ Model structure invalid: {e}")
        return False
    
    # Test inference
    try:
        session = ort.InferenceSession(model_path)
        
        # Print input/output info
        print(f"\n  Inputs:")
        for inp in session.get_inputs():
            print(f"    - {inp.name}: {inp.shape}")
        
        print(f"  Outputs:")
        for out in session.get_outputs():
            print(f"    - {out.name}: {out.shape}")
        
        # Run test inferences with different sequence lengths
        if test_input_shapes is None:
            test_input_shapes = [(1, 50, mel_bins), (1, 100, mel_bins), (1, 200, mel_bins)]
        
        print(f"\n  Test inferences:")
        for batch_size, time_steps, features in test_input_shapes:
            test_input = np.random.randn(batch_size, time_steps, features).astype(np.float32)
            input_name = session.get_inputs()[0].name
            outputs = session.run(None, {input_name: test_input})
            
            # Verify output shapes
            expected_shapes = [
                (batch_size, time_steps, piano_keys),  # onset_probs
                (batch_size, time_steps, piano_keys),  # offset_probs
                (batch_size, time_steps, piano_keys),  # frame_probs
                (batch_size, time_steps, piano_keys)   # velocities
            ]
            
            if len(outputs) != len(expected_shapes):
                print(f"    ✗ Expected {len(expected_shapes)} outputs, got {len(outputs)}")
                return False
            
            all_correct = True
            for i, (actual, expected) in enumerate(zip(outputs, expected_shapes)):
                if actual.shape != expected:
                    print(f"    ✗ Output {i} shape mismatch: {actual.shape} != {expected}")
                    all_correct = False
            
            if all_correct:
                output_str = ", ".join(f"{o.shape}" for o in outputs)
                print(f"    ✓ Shape ({batch_size}, {time_steps}, {features}) → {output_str}")
            else:
                return False
        
        print("\n  ✓ Model verification successful!")
        return True
        
    except Exception as e:
        print(f"  ✗ Inference test failed: {e}")
        raise
