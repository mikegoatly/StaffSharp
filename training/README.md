# StaffSharp ML Training Pipeline

This directory contains the training infrastructure for polyphonic note detection models.

## Overview

The training pipeline uses:
- **C#** for dataset preparation (guarantees training/inference parity)
- **Python (PyTorch)** for model training
- **ONNX** export for .NET inference via ML.NET

```
┌─────────────────────────────────────────────────────────────┐
│  C# Dataset Preparation (StaffSharp.Cli)                    │
│  ─────────────────────────────────────────────              │
│  1. Load MAESTRO audio (WAV) and MIDI files                 │
│  2. Extract mel spectrograms using MelSpectrogramExtractor  │
│  3. Parse MIDI to generate ground truth rolls               │
│  4. Save as .npz files (NumPy format)                       │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Python Training (PyTorch)                                  │
│  ────────────────────────                                   │
│  1. Load preprocessed .npz files                            │
│  2. Train Onsets & Frames model                             │ 
│  3. Save model weights                                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  C# Inference (StaffSharp.MachineLearning)                  │
│  ────────────────────────────────────────                   │
│  1. Use same MelSpectrogramExtractor                        │
│  2. Load model and run inference                            │
│  3. Output transcription                                    │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Prepare Dataset (C#)

Instead of duplicating feature extraction logic in both Python and C#, we use 
**C# as the single source of truth** for dataset preparation. This eliminates the parity issues 
between training and inference. The C# implementation also performed significantly faster than
Python on our benchmarks.

## Architecture

## Usage

### Option 1: Direct C# Invocation (Recommended)

```bash
# Prepare full MAESTRO dataset
dotnet run --project src/StaffSharp.Cli --configuration Release \\
    prepare-dataset \\
    /path/to/maestro-v3.0.0 \\
    training/data/processed

# Test with limited files
dotnet run --project src/StaffSharp.Cli -- \\
    prepare-dataset \\
    /path/to/maestro-v3.0.0 \\
    training/data/processed \\
    --max-files 10 \\
    --parallel 8
```

### Option 2: Python Wrapper

```bash
cd training/scripts
python prepare_dataset_csharp.py \\
    --maestro-dir /path/to/maestro-v3.0.0 \\
    --output-dir ../data/processed \\
    --max-files 10
```

### Training (Unchanged)

```bash
python training/scripts/train.py \\
    --data-dir training/data/processed \\
    --epochs 100 \\
    --batch-size 8
```

## File Format

### Output Structure
```
data/processed/
├── train/
│   ├── track001.npz
│   ├── track002.npz
│   └── ...
├── validation/
│   └── ...
└── test/
    └── ...
```

### NPZ Contents
Each `.npz` file contains:
- `mel_spec`: (time_frames, 229) - Mel spectrogram features
- `piano_roll`: (time_frames, 88) - Active notes (1 = active, 0 = silent)
- `onset_roll`: (time_frames, 88) - Note onsets (1 at onset frame)
- `offset_roll`: (time_frames, 88) - Note offsets (1 at offset frame)
- `velocity_roll`: (time_frames, 88) - Velocity at onset (0.0-1.0)
- `audio_path`: Original audio file path (metadata)
- `midi_path`: Original MIDI file path (metadata)

### Potential Additions
- Audio augmentation during preprocessing
- Multiple sample rates support
- Custom hop size/frame size options
- Real-time preprocessing monitoring
- Checkpointing for interrupted runs


