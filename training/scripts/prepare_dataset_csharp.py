"""
Simplified dataset preparation using C# preprocessing.

Instead of extracting features in Python, this script calls the C# CLI tool
which uses the exact same MelSpectrogramExtractor that will be used for inference.

Usage:
    # Option 1: Call C# directly
    dotnet run --project ../../src/StaffSharp.Cli prepare-dataset <maestro-dir> <output-dir>
    
    # Option 2: Use this wrapper
    python prepare_dataset_csharp.py --maestro-dir /path/to/maestro --output-dir data/processed
"""

import argparse
import subprocess
import sys
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(
        description='Prepare MAESTRO dataset using C# preprocessing'
    )
    parser.add_argument(
        '--maestro-dir',
        type=str,
        required=True,
        help='Path to maestro-v3.0.0 directory'
    )
    parser.add_argument(
        '--output-dir',
        type=str,
        default='data/processed',
        help='Output directory for processed data'
    )
    parser.add_argument(
        '--max-files',
        type=int,
        default=None,
        help='Maximum files per split (for testing)'
    )
    parser.add_argument(
        '--parallel',
        type=int,
        default=None,
        help='Number of parallel processing tasks'
    )

    args = parser.parse_args()

    # Build command
    cli_project = Path(__file__).parent.parent.parent / 'src' / 'StaffSharp.Cli'
    
    cmd = [
        'dotnet', 'run',
        '--project', str(cli_project),
        '--configuration', 'Release',  # Use Release for better performance
        'prepare-dataset',
        args.maestro_dir,
        args.output_dir
    ]

    if args.max_files:
        cmd.extend(['--max-files', str(args.max_files)])
    
    if args.parallel:
        cmd.extend(['--parallel', str(args.parallel)])

    print(f"Running C# data preparation...")
    print(f"Command: {' '.join(cmd)}")
    print()

    # Run the C# CLI tool
    result = subprocess.run(cmd)

    if result.returncode == 0:
        print("\n✓ Dataset preparation complete!")
        print(f"\nProcessed data saved to: {args.output_dir}")
        print("\nYou can now train with:")
        print(f"  python train.py --data-dir {args.output_dir} --epochs 100")
    else:
        print("\n✗ Dataset preparation failed!")
        sys.exit(1)


if __name__ == '__main__':
    main()
