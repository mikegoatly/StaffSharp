"""
Script to load a trained model and visualize its predictions on a random validation sample.
Generates plots comparing the model's predicted onsets/frames against ground truth.
"""

import torch
import matplotlib.pyplot as plt
import numpy as np
from model import create_model
from train import MaestroDataset, collate_fn
import random

# 1. Setup
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'
DATA_DIR = r"../tmp/maestro-v3.0.0-processed"
CHECKPOINT = "models/best_model.pt"

def visualize():
    print("Loading model...")
    # Re-create the architecture
    model = create_model(DEVICE)
    
    # Load the weights
    checkpoint = torch.load(CHECKPOINT, map_location=DEVICE)
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    
    print(f"Loaded checkpoint from Epoch {checkpoint['epoch']}")

    # 2. Get a random sample
    print("Loading one validation sample...")
    val_dataset = MaestroDataset(DATA_DIR, split='validation', sequence_length=600) # Short 6s clip
    
    # Pick a random file
    idx = random.randint(0, len(val_dataset)-1)
    mel, onset_ref, offset_ref, frame_ref, vel_ref = val_dataset[idx]
    
    # Add batch dimension (1, Time, Mels)
    mel = mel.unsqueeze(0).to(DEVICE)
    
    # 3. Predict
    print("Running inference...")
    with torch.no_grad():
        # Get raw probabilities
        onset_pred, offset_pred, frame_pred, vel_pred = model(mel)
        
        # Sigmoid to get 0.0-1.0 range
        onset_prob = torch.sigmoid(onset_pred).squeeze().cpu().numpy()
        frame_prob = torch.sigmoid(frame_pred).squeeze().cpu().numpy()
        
    # Get Ground Truth for comparison
    onset_true = onset_ref.numpy()
    frame_true = frame_ref.numpy()

    # 4. Plot
    print("Plotting...")
    fig, axs = plt.subplots(3, 1, figsize=(15, 12), sharex=True)
    
    # Plot A: The Input (What the model sees)
    axs[0].imshow(mel.squeeze().cpu().numpy().T, aspect='auto', origin='lower', cmap='magma')
    axs[0].set_title("Input Mel Spectrogram (The Audio)")
    axs[0].set_ylabel("Freq Bin")

    # Plot B: The Frame Predictions (Sustain)
    # Green = True Positive, Red = False Positive, Blue = False Negative
    # Simple view: Just the probabilities
    axs[1].imshow(frame_prob.T, aspect='auto', origin='lower', cmap='inferno', vmin=0, vmax=1)
    axs[1].set_title(f"Model Prediction: Notes (Frames) - Epoch {checkpoint['epoch']}")
    axs[1].set_ylabel("Piano Key (0-87)")

    # Plot C: The Ground Truth
    axs[2].imshow(frame_true.T, aspect='auto', origin='lower', cmap='gray_r', vmin=0, vmax=1)
    axs[2].set_title("Ground Truth (The Sheet Music)")
    axs[2].set_ylabel("Piano Key (0-87)")
    axs[2].set_xlabel("Time (Frames)")

    plt.tight_layout()
    plt.show()

if __name__ == '__main__':
    visualize()