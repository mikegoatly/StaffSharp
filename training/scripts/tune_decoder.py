import optuna
import torch
import numpy as np
from torch.utils.data import DataLoader, Subset
from tqdm import tqdm
from functools import partial
import random

# Import your existing code
from train import MaestroDataset, collate_fn
from model import create_model
from metrics import compute_note_metrics

# CONFIG
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'
CHECKPOINT = r"models\2026-02-01_1256\final_model_epoch_65.pt"
DATA_DIR = r"..\tmp\maestro-v3.0.0-processed"
N_VAL_SONGS = 40
SAMPLE_RATE = 16000
HOP_SIZE = 512
FPS = SAMPLE_RATE / HOP_SIZE  # ~31.25

def get_search_ranges():
    ranges = {
        'onset_min': 0.3, 'onset_max': 0.95,
        'frame_min': 0.3, 'frame_max': 0.95,
        'offset_min': 0.3, 'offset_max': 0.95
    }

    return ranges

def process_long_sequence(model, mel_spec_full, device, chunk_size=20000):
    """
    Slices a long song into chunks, processes them on GPU, 
    and stitches them back on CPU to save VRAM.
    """
    total_length = mel_spec_full.shape[1]  # (Batch, Time, Mel)
    
    # Store results on CPU to save GPU memory
    all_onsets = []
    all_offsets = []
    all_frames = []
    all_vels = []
    
    # Iterate in chunks
    for i in range(0, total_length, chunk_size):
        # 1. Slice on CPU (Fast & Cheap)
        chunk = mel_spec_full[:, i:i+chunk_size, :]
        
        # 2. Move ONLY the chunk to GPU
        chunk = chunk.to(device)
        
        # 3. Inference
        with torch.no_grad():
            o, off, f, v = model(chunk)
        
        # 4. Move result back to CPU immediately
        all_onsets.append(o.cpu())
        all_offsets.append(off.cpu())
        all_frames.append(f.cpu())
        all_vels.append(v.cpu())
        
        # 5. Clear GPU cache
        del chunk, o, off, f, v
    
    # Stitch back together
    return (torch.cat(all_onsets, dim=1),
            torch.cat(all_offsets, dim=1),
            torch.cat(all_frames, dim=1),
            torch.cat(all_vels, dim=1))

def get_validation_data():
    val_dataset = MaestroDataset(DATA_DIR, split='validation', sequence_length=None)
    
    # Shuffle and pick a subset to speed up optimization
    indices = list(range(len(val_dataset)))
    random.seed(48) 
    random.shuffle(indices)
    subset_indices = indices[:N_VAL_SONGS]
    
    subset = Subset(val_dataset, subset_indices)
    
    # Batch size 1 because full songs have variable lengths
    return DataLoader(subset, batch_size=1, collate_fn=collate_fn, num_workers=0, pin_memory=False)

def objective(trial, model, dataloader, ranges):
    onset_thresh = trial.suggest_float('onset_thresh', ranges['onset_min'], ranges['onset_max'])
    frame_thresh = trial.suggest_float('frame_thresh', ranges['frame_min'], ranges['frame_max'])
    offset_thresh = trial.suggest_float('offset_thresh', ranges['offset_min'], ranges['offset_max'])
    
    min_velocity = trial.suggest_float('min_velocity', 0.01, 0.15)
    min_duration = trial.suggest_float('min_duration', 0.03, 0.10) # 30ms to 100ms
    gap_tolerance = trial.suggest_float('gap_tolerance', 0.05, 0.2) # 50ms to 200ms
    
    total_f1 = 0
    count = 0
    
    with torch.no_grad():
        for batch in dataloader:
            mel, onset_ref, offset_ref, frame_ref, vel_ref, mask = batch
            
            # Process in chunks to save GPU memory
            o_pred, off_pred, f_pred, v_pred = process_long_sequence(model, mel, DEVICE)
            
            # Sigmoid
            o_prob = torch.sigmoid(o_pred)
            off_prob = torch.sigmoid(off_pred)
            f_prob = torch.sigmoid(f_pred)
            v_pred = torch.sigmoid(v_pred)
            
            # Compute Metric
            metrics = compute_note_metrics(
                o_prob, off_prob, f_prob, v_pred,
                onset_ref, offset_ref, frame_ref, vel_ref,
                onset_thresh=onset_thresh,
                frame_thresh=frame_thresh,
                offset_thresh=offset_thresh,
                min_duration_seconds=min_duration,
                gap_tolerance_seconds=gap_tolerance, 
                min_velocity=min_velocity,
                frame_rate=FPS,  
                mask=mask)
            
            total_f1 += metrics['note_f1']
            count += 1
            
            # Early Pruning: If the first 5 songs average < 0.3 F1, kill the trial
            if count == 5:
                avg_so_far = total_f1 / count
                trial.report(avg_so_far, count)
                if trial.should_prune():
                    raise optuna.TrialPruned()

    return total_f1 / count

if __name__ == '__main__':
    print(f"Using device: {DEVICE}")
    
    # 1. Load Model & Extract Stats
    model = create_model(DEVICE)
    print(f"Loading checkpoint: {CHECKPOINT}")
    checkpoint = torch.load(CHECKPOINT, map_location=DEVICE)
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    
    # 2. Determine Search Ranges from Checkpoint
    metrics = checkpoint.get('metrics', {})
    search_ranges = get_search_ranges()
    print(f"Search Ranges: {search_ranges}")
    
    # 3. Load Data
    dataloader = get_validation_data()
    print(f"Tuning on {N_VAL_SONGS} validation songs.")
    
    # 4. Optimize
    # Use TPE (Tree-structured Parzen Estimator) which is smarter than random search
    sampler = optuna.samplers.TPESampler(seed=42)
    study = optuna.create_study(direction='maximize', sampler=sampler)
    
    obj_func = partial(objective, model=model, dataloader=dataloader, ranges=search_ranges)
    
    print("\nStarting optimization...")
    study.optimize(obj_func, n_trials=100)
    
    print("\n" + "="*60)
    print("BEST PARAMETERS FOR C# CONFIG:")
    print("="*60)
    for k, v in study.best_params.items():
        print(f"  {k}: {v}")
    print(f"\n  Best F1: {study.best_value:.4f}")