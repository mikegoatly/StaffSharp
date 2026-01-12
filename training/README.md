# StaffSharp ML Training Pipeline

This directory contains the Python-based training infrastructure for polyphonic note detection models.

## Overview

The training pipeline uses Python (PyTorch) to train deep learning models for polyphonic music transcription. Trained models are exported to ONNX format for inference in the .NET application via ML.NET.

## Setup

### Prerequisites

- Python 3.10 or higher
- pip package manager
- (Optional) CUDA-capable GPU for faster training

### Installation

1. Create a virtual environment:
```bash
python -m venv venv
```

2. Activate the environment:
```bash
# Windows
venv\Scripts\activate

# Linux/Mac
source venv/bin/activate
```

3. Install dependencies:
```bash
pip install -r requirements.txt
```

4. Verify installation:
```bash
python scripts/feature_extraction.py --help
```

## Directory Structure

```
training/
├── scripts/           # Python training scripts
│   ├── feature_extraction.py    # Mel spectrogram extraction (matches C#)
│   ├── prepare_dataset.py       # Dataset preparation (TODO)
│   ├── train_onsets_frames.py   # Model training (TODO)
│   └── export_to_onnx.py        # ONNX export (TODO)
├── data/              # Training datasets (gitignored)
├── models/            # Trained model checkpoints (gitignored)
├── notebooks/         # Jupyter notebooks for experimentation
└── requirements.txt   # Python dependencies
```

## Feature Extraction

The `feature_extraction.py` script provides a reference implementation of mel spectrogram extraction that MUST match the C# implementation exactly.

### Basic Usage

Extract features from an audio file:
```bash
python scripts/feature_extraction.py audio.wav --output features.npy
```

### Parameters

- `--sample-rate`: Target sample rate in Hz (default: 16000)
- `--mel-bins`: Number of mel frequency bins (default: 229)
- `--output`: Output path for features (.npy file)

### Example

```bash
python scripts/feature_extraction.py piano.wav \
    --sample-rate 16000 \
    --mel-bins 229 \
    --output piano_features.npy
```

## Validating C# Implementation

To ensure the C# `MelSpectrogramExtractor` produces identical output to Python:

1. Extract features using Python:
```bash
python scripts/feature_extraction.py test.wav --output python_features.npy
```

2. Extract features using C# (via unit tests)

3. Compare outputs (should be within floating-point tolerance ~1e-5)

## Datasets

### Recommended Datasets for Piano Transcription

1. **MAESTRO v3.0** (Recommended)
   - 200+ hours of piano performances with aligned MIDI
   - High quality audio and annotations
   - Download: https://magenta.tensorflow.org/datasets/maestro

2. **MAPS Database**
   - Synthetic and real piano recordings
   - Good for data augmentation
   - Download: http://www.tsi.telecom-paristech.fr/aao/en/2010/07/08/maps-database/

### Dataset Preparation

Place downloaded datasets in the `data/` directory:
```
training/data/
├── maestro-v3.0.0/
│   ├── 2004/
│   ├── 2006/
│   └── ...
└── MAPS/
    ├── AkPnBcht/
    ├── AkPnBsdf/
    └── ...
```

## Training Workflow (Coming Soon)

1. **Prepare dataset**:
```bash
python scripts/prepare_dataset.py \
    --dataset maestro \
    --data-dir data/maestro-v3.0.0 \
    --output data/processed
```

2. **Train model**:
```bash
python scripts/train_onsets_frames.py \
    --data data/processed \
    --epochs 100 \
    --batch-size 8 \
    --learning-rate 0.0006
```

3. **Export to ONNX**:
```bash
python scripts/export_to_onnx.py \
    --checkpoint models/best_model.pt \
    --output models/onsets_frames.onnx
```

4. **Copy to C# project**:
```bash
cp models/onsets_frames.onnx ../src/StaffSharp.MachineLearning/models/
```

## Model Architecture

The training pipeline implements the "Onsets and Frames" architecture:

```
Audio (WAV)
  ↓
Mel Spectrogram (16kHz, 229 bins)
  ↓
Acoustic Model (CNN + BiLSTM)
  ├→ Onset Head (88 piano keys)
  ├→ Frame Head (88 piano keys)
  └→ Velocity Head (88 piano keys)
```

### Key Features

- **CNN**: 7 convolutional layers for feature extraction
- **BiLSTM**: 2-layer bidirectional LSTM for temporal modeling
- **Three prediction heads**: Separate outputs for onsets, frames, and velocities
- **88 piano keys**: MIDI notes 21 (A0) through 108 (C8)

### Loss Function

```python
total_loss = onset_loss + frame_loss + velocity_loss
```

- Onset loss: Binary cross-entropy
- Frame loss: Binary cross-entropy
- Velocity loss: MSE (masked to onset frames only)

## Evaluation Metrics

The model will be evaluated using:

1. **Note-level metrics** (50ms onset tolerance):
   - Precision, Recall, F1 score
   - Target: F1 > 85%

2. **Frame-level metrics**:
   - Frame accuracy
   - Target: F1 > 90%

3. **Velocity accuracy**:
   - Mean Absolute Error (MAE)
   - Target: MAE < 10 (on MIDI 0-127 scale)

## Development

### Running Tests

```bash
pytest scripts/
```

### Jupyter Notebooks

Start Jupyter for interactive development:
```bash
jupyter notebook notebooks/
```

## Troubleshooting

### CUDA Out of Memory

If you encounter GPU memory errors:
- Reduce batch size
- Use gradient accumulation
- Enable mixed precision training

### Slow Training

- Use GPU if available
- Reduce model size for prototyping
- Use smaller dataset for initial experiments

### Feature Mismatch (C# vs Python)

If C# and Python features don't match:
1. Check sample rate conversion
2. Verify FFT parameters (frame size, hop size)
3. Compare mel filterbank construction
4. Check log compression constant

## References

- [Onsets and Frames: Dual-Objective Piano Transcription](https://arxiv.org/abs/1710.11153)
- [MAESTRO Dataset](https://magenta.tensorflow.org/datasets/maestro)
- [librosa Documentation](https://librosa.org/doc/latest/)
- [ONNX Runtime](https://onnxruntime.ai/)

## Next Steps

1. ✅ Feature extraction reference implementation
2. ⬜ Dataset preparation script
3. ⬜ Model training implementation
4. ⬜ ONNX export script
5. ⬜ Evaluation notebooks
6. ⬜ Model optimization and tuning
