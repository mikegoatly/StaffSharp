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
CHECKPOINT = "models/best_f1.pt"
DATA_DIR = r"..\tmp\maestro-v3.0.0-processed"
N_VAL_SONGS = 25
SAMPLE_RATE = 16000
HOP_SIZE = 512
FPS = SAMPLE_RATE / HOP_SIZE  # ~31.25

def get_smart_ranges(metrics):
    """
    Analyze checkpoint metrics to determine the optimal search area.
    """
    ranges = {
        'onset_min': 0.1, 'onset_max': 0.9,
        'frame_min': 0.1, 'frame_max': 0.9,
        'offset_min': 0.1, 'offset_max': 0.9
    }
    
    # Check Onset P/R Balance
    # 'eval_metrics' key might vary based on how you saved it, checking standard locations
    eval_m = metrics.get('eval_metrics', {})
    
    on_prec = eval_m.get('onset_precision', 0.5)
    on_rec = eval_m.get('onset_recall', 0.5)
    off_prec = eval_m.get('offset_precision', 0.5)
    off_rec = eval_m.get('offset_recall', 0.5)
    
    print(f" Onset P/R: {on_prec:.2f} / {on_rec:.2f}")

    # Logic: If Recall is much higher than Precision, we are "Trigger Happy".
    # We need to Shift the search window UP to filter false positives.
    if on_rec > on_prec + 0.2:
        print("  -> Model is Trigger Happy (High Recall). Shifting search range UP.")
        ranges['onset_min'] = 0.4
        ranges['onset_max'] = 0.95
        ranges['frame_min'] = 0.4
        ranges['frame_max'] = 0.95
    # Logic: If Precision is much higher, we are "Shy". Shift DOWN.
    elif on_prec > on_rec + 0.2:
        print("  -> Model is Shy (High Precision). Shifting search range DOWN.")
        ranges['onset_min'] = 0.05
        ranges['onset_max'] = 0.6
        ranges['frame_min'] = 0.05
        ranges['frame_max'] = 0.6

    print(f"  Offset P/R: {off_prec:.2f} / {off_rec:.2f}")
    
    if off_rec > off_prec + 0.2:
        print("  -> Offsets are Trigger Happy (Jittery). Shifting range UP.")
        ranges['offset_min'] = 0.5
        ranges['offset_max'] = 0.98
    elif off_prec > off_rec + 0.2:
        print("  -> Offsets are Shy (Notes stick). Shifting range DOWN.")
        ranges['offset_min'] = 0.05
        ranges['offset_max'] = 0.5

    return ranges

def get_validation_data():
    val_dataset = MaestroDataset(DATA_DIR, split='validation', sequence_length=None)
    
    # Shuffle and pick a subset to speed up optimization
    indices = list(range(len(val_dataset)))
    random.shuffle(indices)
    subset_indices = indices[:N_VAL_SONGS]
    
    subset = Subset(val_dataset, subset_indices)
    
    # Batch size 1 because full songs have variable lengths
    return DataLoader(subset, batch_size=1, collate_fn=collate_fn, num_workers=0)

def objective(trial, model, dataloader, ranges):
    onset_thresh = trial.suggest_float('onset_thresh', ranges['onset_min'], ranges['onset_max'])
    frame_thresh = trial.suggest_float('frame_thresh', ranges['frame_min'], ranges['frame_max'])
    offset_thresh = trial.suggest_float('offset_thresh', ranges['offset_min'], ranges['offset_max'])
    
    min_velocity = trial.suggest_float('min_velocity', 0.01, 0.15)
    min_duration = trial.suggest_float('min_duration', 0.03, 0.10) # 30ms to 100ms
    gap_tolerance = trial.suggest_float('gap_tolerance', 0.05, 0.2) # 50ms to 200ms
    min_frame_for_onset = trial.suggest_float('min_frame_for_onset', 0.1, 0.5)
    
    total_f1 = 0
    count = 0
    
    with torch.no_grad():
        for batch in dataloader:
            mel, onset_ref, offset_ref, frame_ref, vel_ref, mask = batch
            mel = mel.to(DEVICE)
            
            # Inference
            o_pred, off_pred, f_pred, v_pred = model(mel)
            
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
                min_frame_for_onset=min_frame_for_onset,
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
    search_ranges = get_smart_ranges(metrics)
    print(f"Search Ranges: {search_ranges}")
    
    # 3. Load Data
    dataloader = get_validation_data()
    print(f"Tuning on {N_VAL_SONGS} validation songs.")
    
    # 4. Optimize
    # Use TPE (Tree-structured Parzen Estimator) which is smarter than random search
    sampler = optuna.samplers.TPESampler(seed=42)
    study = optuna.create_study(direction='maximize', sampler=sampler)
    
    # 5. Inject Initial Guesses
    # Choose starting thresholds based on BOTH onset and offset diagnoses
    onset_start = 0.7 if search_ranges['onset_min'] > 0.3 else 0.3 if search_ranges['onset_max'] < 0.7 else 0.5
    frame_start = 0.6 if search_ranges['frame_min'] > 0.3 else 0.3 if search_ranges['frame_max'] < 0.7 else 0.5
    offset_start = 0.7 if search_ranges['offset_min'] > 0.4 else 0.3 if search_ranges['offset_max'] < 0.6 else 0.5
    
    print(f"Injecting initial guess: onset={onset_start}, frame={frame_start}, offset={offset_start}")
    study.enqueue_trial({
        'onset_thresh': onset_start,
        'frame_thresh': frame_start,
        'offset_thresh': offset_start,
        'min_velocity': 0.05,
        'min_duration': 0.05,
        'gap_tolerance': 0.1,
        'min_frame_for_onset': 0.3
    })
    
    obj_func = partial(objective, model=model, dataloader=dataloader, ranges=search_ranges)
    
    print("\nStarting optimization...")
    study.optimize(obj_func, n_trials=100)
    
    print("\n" + "="*60)
    print("BEST PARAMETERS FOR C# CONFIG:")
    print("="*60)
    for k, v in study.best_params.items():
        print(f"  {k}: {v}")
    print(f"\n  Best F1: {study.best_value:.4f}")