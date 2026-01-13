# Test Data

This directory contains test fixtures for the machine learning tests.

## test_model.onnx

A minimal dummy ONNX model for testing the `OnnxPolyphonicTranscriber` inference pipeline.

**Purpose**: Tests model loading, inference execution, and output validation without requiring a trained model.

**Architecture**: Simple linear projections that match the "Onsets and Frames" model signature:
- **Input**: `(batch, time, 229)` - mel spectrogram features
- **Outputs**:
  - `onset_probs`: `(batch, time, 88)` - onset probabilities [0-1]
  - `frame_probs`: `(batch, time, 88)` - frame activation probabilities [0-1]
  - `velocities`: `(batch, time, 88)` - normalized velocities [0-1]

**Note**: This model has **random weights** and produces meaningless predictions. It's only for testing infrastructure, not actual transcription.

## Regenerating the Test Model

If you need to regenerate `test_model.onnx` (e.g., to change architecture or fix issues):

### Prerequisites

Install Python dependencies:
```bash
pip install -r training/requirements.txt
```

### Generate the Model

From the repository root:
```bash
python training/scripts/create_test_model.py --output test/StaffSharp.MachineLearning.Tests/TestData/test_model.onnx
```

The script will:
1. Create a minimal PyTorch model with the correct signature
2. Export it to ONNX format with dynamic axes (batch and time dimensions)
3. Verify the exported model loads and runs correctly

### Options

```bash
# Customize mel bins or piano keys (if architecture changes)
python training/scripts/create_test_model.py \
  --output test/StaffSharp.MachineLearning.Tests/TestData/test_model.onnx \
  --mel-bins 229 \
  --piano-keys 88
```

## For Real Transcription

This test model is **not suitable** for actual music transcription. For a trained model:
1. Follow the training instructions in `training/README.md` (Phase 5)
2. Train on the MAESTRO dataset
3. Export the trained model to ONNX
4. Use the trained model with `OnnxPolyphonicTranscriber` in production

## Files

- `test_model.onnx` - Model structure and metadata (11KB)
- `test_model.onnx.data` - Model parameters/weights (238KB)

Both files are required and committed to the repository for CI/CD and developer convenience.
