"""
Shared utilities for ONNX model export and verification.

This module provides common functionality used by both export_to_onnx.py
and create_test_model.py to avoid code duplication.
"""

from pathlib import Path
from typing import Tuple, List, Optional

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
    # Using dynamo=False for more stable export with dynamic_axes
    # The newer dynamo=True requires dynamic_shapes API which is more complex
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
        verbose=verbose,
        dynamo=False  # Use older export path for stability with dynamic_axes
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
        print("  Model structure is valid")
    except Exception as e:
        print(f"  ERROR: Model structure invalid: {e}")
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
                print(f"    ERROR: Expected {len(expected_shapes)} outputs, got {len(outputs)}")
                return False
            
            all_correct = True
            for i, (actual, expected) in enumerate(zip(outputs, expected_shapes)):
                if actual.shape != expected:
                    print(f"    ERROR: Output {i} shape mismatch: {actual.shape} != {expected}")
                    all_correct = False
            
            if all_correct:
                output_str = ", ".join(f"{o.shape}" for o in outputs)
                print(f"    OK: Shape ({batch_size}, {time_steps}, {features}) -> {output_str}")
            else:
                return False
        
        print("\n  Model verification successful!")
        return True
        
    except Exception as e:
        print(f"  ERROR: Inference test failed: {e}")
        raise


def quantize_onnx_model(
    input_path: str,
    output_path: str,
    quantization_mode: str = 'dynamic',
    calibration_data: Optional[np.ndarray] = None
) -> None:
    """
    Quantize an ONNX model to reduce file size.

    Args:
        input_path: Path to input ONNX model
        output_path: Path to save quantized model
        quantization_mode: Quantization mode - 'float16', 'dynamic', or 'static'
        calibration_data: Calibration data for static quantization (optional)

    Quantization modes:
        - 'float16': Convert FP32 to FP16 (~50% size reduction, minimal accuracy loss)
        - 'dynamic': INT8 weights, FP32 activations (~75% size reduction, some accuracy loss)
        - 'static': INT8 weights and activations (best compression, requires calibration)

    Raises:
        ImportError: If onnxruntime quantization tools not available
        ValueError: If invalid quantization mode specified
    """
    try:
        from onnxruntime.quantization import quantize_dynamic, quantize_static, QuantType
        from onnxruntime.quantization.calibrate import CalibrationDataReader
        import onnx
        from onnx import version_converter
    except ImportError as e:
        print(f"Quantization skipped: {e}")
        print("Install onnxruntime with: pip install onnxruntime")
        return

    print(f"\nQuantizing model: {input_path}")
    print(f"  Mode: {quantization_mode}")

    # Get original file size
    original_size = Path(input_path).stat().st_size / 1024 / 1024

    if quantization_mode == 'float16':
        # Float16 quantization - simple and effective
        import onnx
        from onnx import numpy_helper

        model = onnx.load(input_path)

        # Try to use onnxconverter-common if available, otherwise use fallback
        try:
            from onnxconverter_common import float16
            model_fp16 = float16.convert_float_to_float16(model)
            onnx.save(model_fp16, output_path)
            print(f"  Converted to float16 using onnxconverter-common")
        except ImportError:
            print("  Using basic float16 conversion (onnxconverter-common not available)")
            # Fallback: manual conversion
            for tensor in model.graph.initializer:
                if tensor.data_type == 1:  # FLOAT
                    float_data = numpy_helper.to_array(tensor)
                    float16_data = float_data.astype(np.float16)
                    new_tensor = numpy_helper.from_array(float16_data, tensor.name)
                    tensor.CopyFrom(new_tensor)
                    tensor.data_type = 10  # FLOAT16
            onnx.save(model, output_path)
            print(f"  Converted to float16")

    elif quantization_mode == 'dynamic':
        # Dynamic quantization - converts weights to INT8
        quantize_dynamic(
            model_input=input_path,
            model_output=output_path,
            weight_type=QuantType.QUInt8
        )
        print(f"  Applied dynamic quantization (INT8 weights)")

    elif quantization_mode == 'static':
        if calibration_data is None:
            raise ValueError("Static quantization requires calibration_data")

        # Static quantization requires calibration data reader
        class DataReader(CalibrationDataReader):
            def __init__(self, data):
                self.data = data
                self.index = 0

            def get_next(self):
                if self.index >= len(self.data):
                    return None
                result = {'input': self.data[self.index]}
                self.index += 1
                return result

        reader = DataReader(calibration_data)
        quantize_static(
            model_input=input_path,
            model_output=output_path,
            calibration_data_reader=reader,
            weight_type=QuantType.QUInt8,
            activation_type=QuantType.QUInt8
        )
        print(f"  Applied static quantization (INT8 weights and activations)")

    else:
        raise ValueError(f"Invalid quantization mode: {quantization_mode}. "
                        f"Use 'float16', 'dynamic', or 'static'")

    # Compare file sizes
    quantized_size = Path(output_path).stat().st_size / 1024 / 1024
    reduction = (1 - quantized_size / original_size) * 100

    print(f"\n  Size comparison:")
    print(f"    Original:  {original_size:.2f} MB")
    print(f"    Quantized: {quantized_size:.2f} MB")
    print(f"    Reduction: {reduction:.1f}%")
