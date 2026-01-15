"""
Early stopping utilities for training.

Monitors validation loss and stops training when it stops improving,
saving training time and preventing overfitting.
"""

from pathlib import Path
from typing import Optional


class EarlyStopping:
    """
    Early stopping monitor that tracks validation loss and stops training
    when the loss plateaus.
    
    This prevents overfitting and saves training time by stopping when
    the model stops improving on the validation set.
    
    Example:
        ```python
        early_stopping = EarlyStopping(patience=5, min_delta=0.001)
        
        for epoch in range(num_epochs):
            train_metrics = train_epoch(...)
            val_metrics = validate(...)
            
            if early_stopping(val_metrics['loss']):
                print("Early stopping triggered!")
                break
        ```
    """
    
    def __init__(
        self,
        patience: int = 10,
        min_delta: float = 0.001,
        checkpoint_dir: Optional[str] = None
    ):
        """
        Initialize early stopping monitor.
        
        Args:
            patience: Number of epochs with no improvement after which training will be stopped
                     (default: 10)
            min_delta: Minimum change in monitored quantity to qualify as an improvement
                      (default: 0.001). Loss must decrease by at least this amount to reset counter.
            checkpoint_dir: Optional directory to save best model checkpoint
                           (default: None, no checkpoint saved)
        """
        self.patience = patience
        self.min_delta = min_delta
        self.checkpoint_dir = Path(checkpoint_dir) if checkpoint_dir else None
        
        self.counter = 0
        self.best_loss = None
        self.best_epoch = None
    
    def __call__(self, val_loss: float, epoch: int = None) -> bool:
        """
        Check if training should stop.
        
        Args:
            val_loss: Current validation loss value
            epoch: Current epoch number (optional, for logging)
        
        Returns:
            True if training should stop, False otherwise
        """
        if self.best_loss is None:
            self.best_loss = val_loss
            self.best_epoch = epoch
            return False
        
        # Check if loss improved
        if val_loss < self.best_loss - self.min_delta:
            # Loss improved - reset counter and update best
            self.best_loss = val_loss
            self.best_epoch = epoch
            self.counter = 0
            return False
        else:
            # Loss did not improve - increment counter
            self.counter += 1
            if self.counter >= self.patience:
                return True
            return False
    
    def get_status(self) -> str:
        """
        Get current early stopping status.
        
        Returns:
            String with formatted early stopping status
        """
        return (
            f"EarlyStopping - "
            f"Best Loss: {self.best_loss:.6f} (epoch {self.best_epoch}), "
            f"Counter: {self.counter}/{self.patience}"
        )
    
    def reset(self):
        """Reset early stopping state."""
        self.counter = 0
        self.best_loss = None
        self.best_epoch = None
