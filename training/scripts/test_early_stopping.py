"""
Test early stopping mechanism.
"""
from early_stopping import EarlyStopping

class TestEarlyStopping:
    def test_early_stopping_initialization(self):
        es = EarlyStopping(patience=10, min_delta=0.001)
        assert es.patience == 10
        assert es.min_delta == 0.001
        assert es.best_loss is None

    def test_early_stopping_no_improvement(self):
        es = EarlyStopping(patience=2, min_delta=0.001)
        assert not es(1.0, 0)
        assert not es(1.0, 1)
        assert es(1.0, 2)

    def test_early_stopping_with_improvement(self):
        es = EarlyStopping(patience=2, min_delta=0.001)
        assert not es(1.0, 0)
        assert not es(0.95, 1)
        assert not es(0.90, 2)
        assert not es(0.85, 3)
        assert not es(0.80, 4)

    def test_early_stopping_reset(self):
        es = EarlyStopping(patience=2, min_delta=0.001)
        es(1.0, 0)
        es(1.0, 1)
        es(1.0, 2)
        es.reset()
        assert not es(1.0, 3)

    def test_early_stopping_status(self):
        es = EarlyStopping(patience=2, min_delta=0.001)
        es(1.0, 0)
        es(0.9, 1)
        status = es.get_status()
        assert isinstance(status, str)
        assert 'EarlyStopping' in status or 'best' in status.lower() or 'loss' in status.lower()
