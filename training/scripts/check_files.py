"""
Script to check the integrity of .npz files in a specified directory.
It attempts to load each file and access key arrays to ensure they are not corrupt.
"""

import numpy as np
from pathlib import Path
from tqdm import tqdm
import os

# Point this to your data directory
DATA_DIR = r"../tmp/maestro-v3.0.0-processed/train"

def check_files():
    files = list(Path(DATA_DIR).glob("*.npz"))
    print(f"Scanning {len(files)} files...")
    
    bad_files = []
    
    for f in tqdm(files):
        try:
            # Try to load and access the keys to ensure data integrity
            with np.load(f) as data:
                _ = data['mel_spec']
                _ = data['piano_roll']
                _ = data['onset_roll']
                _ = data['offset_roll']
                _ = data['velocity_roll']
        except Exception as e:
            print(f"\n[CORRUPT] Found bad file: {f}")
            print(f"Reason: {e}")
            bad_files.append(f)

    if bad_files:
        print(f"\nFound {len(bad_files)} corrupt files.")

        if input("Do you want to delete these files? (y/n): ").lower() == 'y':
            for f in bad_files:
                try:
                    os.remove(f)
                    print(f"Deleted: {f.name}")
                except Exception as e:
                    print(f"Could not delete {f.name}: {e}")
    else:
        print("\nAll files look good!")

if __name__ == "__main__":
    check_files()