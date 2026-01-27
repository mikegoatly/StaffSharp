import optuna
import torch
from torch.utils.data import DataLoader
from tqdm import tqdm
from functools import partial

# Import your existing code
from train import MaestroDataset, collate_fn  # Your dataset logic
from model import create_model                # Your model logic
from metrics import compute_note_metrics      # Your metric logic (which calls decode_notes)

# CONFIG
DEVICE = 'cpu'
CHECKPOINT = "models/final_model_epoch_70.pt"
DATA_DIR = r"..\tmp\maestro-v3.0.0-processed"

def get_validation_data():
    # Load a subset of validation data to tune on (e.g. 20-50 songs)
    val_dataset = MaestroDataset(DATA_DIR, split='validation', sequence_length=None) 
    # Use batch_size=1 because sequence lengths vary in full songs
    return DataLoader(val_dataset, batch_size=1, collate_fn=collate_fn, num_workers=4)

def objective(trial, model, dataloader):
    # 1. Suggest Parameters to try
    onset_thresh = trial.suggest_float('onset_thresh', 0.1, 0.9)
    frame_thresh = trial.suggest_float('frame_thresh', 0.1, 0.9)
    offset_thresh = trial.suggest_float('offset_thresh', 0.1, 0.9)
    
    min_velocity = trial.suggest_float('min_velocity', 0.01, 0.2)
    min_duration = trial.suggest_float('min_duration', 0.05, 0.15)
    gap_tolerance = trial.suggest_float('gap_tolerance', 0.01, 0.2)
    min_frame_for_onset = trial.suggest_float('min_frame_for_onset', 0.1, 0.5)
    
    # 2. Run Evaluation on the dataset
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
            v_pred = torch.sigmoid(v_pred) # If velocity wasn't sigmoid-ed in model
            
            # Compute Metric with THESE SPECIFIC THRESHOLDS
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
            )
            
            total_f1 += metrics['note_f1']
            count += 1
            
            # Pruning: If this trial is doing terrible early on, stop it to save time
            trial.report(total_f1 / count, count)
            if trial.should_prune():
                raise optuna.TrialPruned()

    return total_f1 / count

if __name__ == '__main__':
    # 1. Load Model Once
    model = create_model(DEVICE)
    checkpoint = torch.load(CHECKPOINT, map_location=DEVICE)
    model.load_state_dict(checkpoint['model_state_dict'])
    model.eval()
    
    # 2. Load Data Once
    dataloader = get_validation_data()
    
    # 3. Optimize
    study = optuna.create_study(direction='maximize')
    
    # Wrap objective to pass in static args
    obj_func = partial(objective, model=model, dataloader=dataloader)
    
    print("Starting optimization...")
    study.optimize(obj_func, n_trials=100)
    
    print("\nBest Parameters found:")
    print(study.best_params)
    print(f"Best F1: {study.best_value}")
