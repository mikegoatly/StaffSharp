"""
Validate that C# and Python mel spectrogram implementations produce identical output.

Usage:
    python validate_features.py <audio_file> <csharp_features_file>

Example:
    python validate_features.py test.wav csharp_mel_features.txt
"""

import sys
import numpy as np
from pathlib import Path
from feature_extraction import MelSpectrogramExtractor, load_audio


def load_csharp_features(file_path: str) -> np.ndarray:
    """Load features exported from C# in text format."""
    with open(file_path, 'r') as f:
        # Read dimensions from first line
        dims = f.readline().strip().split()
        rows, cols = int(dims[0]), int(dims[1])

        # Read data
        features = []
        for line in f:
            values = [float(x) for x in line.strip().split()]
            features.append(values)

        return np.array(features, dtype=np.float32)


def compare_features(python_features: np.ndarray, csharp_features: np.ndarray, tolerance: float = 1e-5):
    """Compare Python and C# features, reporting differences."""

    print(f"\n{'='*60}")
    print("FEATURE COMPARISON")
    print(f"{'='*60}")

    # Check shapes
    print(f"\nShape comparison:")
    print(f"  Python: {python_features.shape}")
    print(f"  C#:     {csharp_features.shape}")

    if python_features.shape != csharp_features.shape:
        print("\n❌ FAILED: Shapes do not match!")
        return False

    # Check data types
    print(f"\nData types:")
    print(f"  Python: {python_features.dtype}")
    print(f"  C#:     {csharp_features.dtype}")

    # Compute differences
    abs_diff = np.abs(python_features - csharp_features)
    max_diff = np.max(abs_diff)
    mean_diff = np.mean(abs_diff)
    median_diff = np.median(abs_diff)

    print(f"\nAbsolute differences:")
    print(f"  Max:    {max_diff:.2e}")
    print(f"  Mean:   {mean_diff:.2e}")
    print(f"  Median: {median_diff:.2e}")

    # Compute relative differences (where values are non-zero)
    python_nonzero = python_features != 0
    if np.any(python_nonzero):
        rel_diff = np.abs((python_features - csharp_features) / (python_features + 1e-10))
        max_rel_diff = np.max(rel_diff[python_nonzero])
        mean_rel_diff = np.mean(rel_diff[python_nonzero])
        print(f"\nRelative differences (non-zero values):")
        print(f"  Max:    {max_rel_diff:.2e}")
        print(f"  Mean:   {mean_rel_diff:.2e}")

    # Check if within tolerance
    print(f"\nTolerance check (threshold: {tolerance:.2e}):")
    if max_diff <= tolerance:
        print(f"  PASSED: Max difference ({max_diff:.2e}) is within tolerance")
        passed = True
    else:
        print(f"  FAILED: Max difference ({max_diff:.2e}) exceeds tolerance")
        passed = False

        # Find and report worst mismatches
        worst_indices = np.unravel_index(np.argsort(abs_diff.ravel())[-5:], abs_diff.shape)
        print(f"\n  Top 5 worst mismatches:")
        for i in range(5):
            t, m = worst_indices[0][-(i+1)], worst_indices[1][-(i+1)]
            py_val = python_features[t, m]
            cs_val = csharp_features[t, m]
            diff = abs_diff[t, m]
            print(f"    [{t:3d}, {m:3d}] Python={py_val:.6f}, C#={cs_val:.6f}, diff={diff:.2e}")

    # Summary statistics
    print(f"\nSummary statistics:")
    print(f"  Python - min: {np.min(python_features):.6f}, max: {np.max(python_features):.6f}, mean: {np.mean(python_features):.6f}")
    print(f"  C#     - min: {np.min(csharp_features):.6f}, max: {np.max(csharp_features):.6f}, mean: {np.mean(csharp_features):.6f}")

    print(f"\n{'='*60}")

    return passed


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)

    audio_path = sys.argv[1]
    csharp_features_path = sys.argv[2]

    # Validate files exist
    if not Path(audio_path).exists():
        print(f"Error: Audio file not found: {audio_path}")
        sys.exit(1)

    if not Path(csharp_features_path).exists():
        print(f"Error: C# features file not found: {csharp_features_path}")
        sys.exit(1)

    print(f"Audio file: {audio_path}")
    print(f"C# features file: {csharp_features_path}")

    # Load C# features
    print("\nLoading C# features...")
    csharp_features = load_csharp_features(csharp_features_path)
    print(f"Loaded C# features with shape: {csharp_features.shape}")

    # Load audio and extract features with Python
    print("\nLoading audio...")
    audio, sr = load_audio(audio_path, sample_rate=16000)
    print(f"Audio: {len(audio)} samples at {sr} Hz ({len(audio)/sr:.2f} seconds)")

    print("\nExtracting features with Python...")
    extractor = MelSpectrogramExtractor()
    python_features = extractor.extract_features(audio, sr)
    print(f"Extracted Python features with shape: {python_features.shape}")

    # Compare
    passed = compare_features(python_features, csharp_features)

    if passed:
        print("\nVALIDATION PASSED: C# and Python implementations match!")
        sys.exit(0)
    else:
        print("\nVALIDATION FAILED: Implementations do not match")
        sys.exit(1)


if __name__ == "__main__":
    main()
