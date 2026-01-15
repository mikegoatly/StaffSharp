"""
Test data loading and preprocessing.
"""
from test_helpers import create_batch

class TestDataPipeline:
    def test_batch_creation(self):
        mel_specs, onset_targets, offset_targets, frame_targets, velocity_targets, mask = create_batch()
        assert mel_specs.shape == (4, 500, 229)
        assert onset_targets.shape == (4, 500, 88)
        assert offset_targets.shape == (4, 500, 88)
        assert frame_targets.shape == (4, 500, 88)
        assert velocity_targets.shape == (4, 500, 88)
        assert mask.shape == (4, 500)
        assert (onset_targets >= 0).all() and (onset_targets <= 1).all()
        assert (offset_targets >= 0).all() and (offset_targets <= 1).all()
        assert (frame_targets >= 0).all() and (frame_targets <= 1).all()
        assert (velocity_targets >= 0).all() and (velocity_targets <= 1).all()
        assert (mask >= 0).all() and (mask <= 1).all()

    def test_variable_sequence_length_batch(self):
        for seq_len in [100, 500, 2000, 4096]:
            mel_specs, onset_targets, _, _, _, _ = create_batch(sequence_length=seq_len)
            assert mel_specs.shape[1] == seq_len
            assert onset_targets.shape[1] == seq_len
