"""
Reference implementation for mel spectrogram feature extraction.
This implementation MUST produce identical output to the C# MelSpectrogramExtractor.

Used for:
1. Validating C# implementation matches Python
2. Preprocessing training data
3. Research and experimentation
"""

import numpy as np
import librosa
from typing import Tuple


class MelSpectrogramExtractor:
    """
    Mel spectrogram extractor with parameters matching C# implementation.

    Default parameters match the Onsets and Frames model:
    - Sample rate: 16000 Hz
    - Frame size: 2048 samples
    - Hop size: 512 samples (~31.25 fps)
    - Mel bins: 229 (piano range A0-C8)
    - Frequency range: 27.5 Hz to 4186 Hz
    """

    def __init__(
        self,
        sample_rate: int = 16000,
        frame_size: int = 2048,
        hop_size: int = 512,
        mel_bins: int = 229,
        min_frequency: float = 27.5,
        max_frequency: float = 4186.0,
        log_compression_constant: float = 10000.0
    ):
        self.sample_rate = sample_rate
        self.frame_size = frame_size
        self.hop_size = hop_size
        self.mel_bins = mel_bins
        self.min_frequency = min_frequency
        self.max_frequency = max_frequency
        self.log_compression_constant = log_compression_constant

    def extract_features(self, audio: np.ndarray, sr: int) -> np.ndarray:
        """
        Extract mel spectrogram features from audio.

        Args:
            audio: Audio samples as numpy array (mono)
            sr: Sample rate of input audio

        Returns:
            Mel spectrogram with shape (time_frames, mel_bins)
        """
        # 1. Resample to target sample rate if needed
        if sr != self.sample_rate:
            audio = librosa.resample(
                audio,
                orig_sr=sr,
                target_sr=self.sample_rate,
                res_type='linear'  # Match C# linear interpolation
            )

        # 2. Compute mel spectrogram
        # Note: librosa uses power=2.0 by default, which is what we want
        mel_spec = librosa.feature.melspectrogram(
            y=audio,
            sr=self.sample_rate,
            n_fft=self.frame_size,
            hop_length=self.hop_size,
            n_mels=self.mel_bins,
            fmin=self.min_frequency,
            fmax=self.max_frequency,
            window='hann',
            center=False,  # Match C# implementation (no padding)
            power=2.0  # Use power spectrogram (magnitude squared)
        )

        # 3. Apply logarithmic compression
        mel_spec = np.log(1 + self.log_compression_constant * mel_spec)

        # 4. Transpose to (time, mels) format
        mel_spec = mel_spec.T

        return mel_spec


def load_audio(audio_path: str, sample_rate: int = 16000) -> Tuple[np.ndarray, int]:
    """
    Load audio file and convert to mono.

    Args:
        audio_path: Path to audio file
        sample_rate: Target sample rate (None = keep original)

    Returns:
        Tuple of (audio_samples, sample_rate)
    """
    audio, sr = librosa.load(audio_path, sr=sample_rate, mono=True)
    return audio, sr


def save_features(features: np.ndarray, output_path: str):
    """Save features to numpy file."""
    np.save(output_path, features)


def load_features(input_path: str) -> np.ndarray:
    """Load features from numpy file."""
    return np.load(input_path)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Extract mel spectrogram features from audio"
    )
    parser.add_argument(
        "audio_path",
        type=str,
        help="Path to input audio file (WAV, MP3, etc.)"
    )
    parser.add_argument(
        "--output",
        type=str,
        default=None,
        help="Path to save features (.npy file). If not specified, prints shape only."
    )
    parser.add_argument(
        "--sample-rate",
        type=int,
        default=16000,
        help="Target sample rate in Hz (default: 16000)"
    )
    parser.add_argument(
        "--mel-bins",
        type=int,
        default=229,
        help="Number of mel bins (default: 229)"
    )

    args = parser.parse_args()

    # Load audio
    print(f"Loading audio from {args.audio_path}...")
    audio, sr = load_audio(args.audio_path, args.sample_rate)
    print(f"Audio shape: {audio.shape}, Sample rate: {sr} Hz")
    print(f"Duration: {len(audio) / sr:.2f} seconds")

    # Extract features
    print("\nExtracting mel spectrogram features...")
    extractor = MelSpectrogramExtractor(
        sample_rate=args.sample_rate,
        mel_bins=args.mel_bins
    )
    features = extractor.extract_features(audio, sr)

    print(f"Feature shape: {features.shape}")
    print(f"  Time frames: {features.shape[0]}")
    print(f"  Mel bins: {features.shape[1]}")
    print(f"  Frame rate: {sr / 512:.2f} fps")

    # Save if output path specified
    if args.output:
        save_features(features, args.output)
        print(f"\nFeatures saved to {args.output}")
    else:
        print("\nNo output path specified. Features not saved.")
        print("Use --output to save features to a file.")
