"""
Equivalence tests for Python and C# NoteEventDecoder implementations.

This test suite verifies that:
1. Python decode_notes() produces identical output to C# NoteEventDecoder
2. Both handle edge cases consistently
3. Frame rate and threshold parameters work the same way
4. Minimum duration filtering is identical

The test data is also exported to JSON for use in C# tests.
"""

import numpy as np
import json
from pathlib import Path
from decode_notes import decode_notes


class TestDecoderEquivalence:
    """Test suite verifying Python decoder matches C# implementation."""
    
    @staticmethod
    def create_test_data_dir():
        """Create directory for test data if it doesn't exist."""
        test_data_dir = Path(__file__).parent / "test_data" / "decoder_equivalence"
        test_data_dir.mkdir(parents=True, exist_ok=True)
        return test_data_dir
    
    @staticmethod
    def export_test_case(name: str, onset_probs, frame_probs, offset_probs, velocity_values, 
                         expected_notes, frame_rate=100.0, thresholds=None):
        """
        Export a test case to JSON for use in C# tests.
        
        Args:
            name: Test case name
            onset_probs: (frames, 88)
            frame_probs: (frames, 88)
            offset_probs: (frames, 88)
            velocity_values: (frames, 88)
            expected_notes: List of expected note dicts
            frame_rate: Frame rate in Hz
            thresholds: Dict with onset_thresh, frame_thresh, offset_thresh
        """
        if thresholds is None:
            thresholds = {'onset_thresh': 0.5, 'frame_thresh': 0.5, 'offset_thresh': 0.5}
        
        test_data = {
            'name': name,
            'frame_rate': frame_rate,
            'thresholds': thresholds,
            'input': {
                'onset_probs': onset_probs.tolist(),
                'frame_probs': frame_probs.tolist(),
                'offset_probs': offset_probs.tolist(),
                'velocity_values': velocity_values.tolist(),
            },
            'expected_notes': expected_notes,
        }
        
        test_data_dir = TestDecoderEquivalence.create_test_data_dir()
        output_path = test_data_dir / f"{name}.json"
        
        with open(output_path, 'w') as f:
            json.dump(test_data, f, indent=2)
        
        print(f"Exported test case: {output_path}")
        return output_path
    
    def test_single_note(self):
        """Test decoding a single note."""
        # Create test data: C4 (MIDI 60 = key index 39) active for 10 frames
        num_frames = 20
        key_index = 39  # C4
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # Onset at frame 5, velocity 0.8
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 0.8
        
        # Active frames 5-14 (10 frames)
        for i in range(5, 15):
            frame_probs[i, key_index] = 1.0
        
        # Decode using exact frame rate (16000 Hz / 512 hop size = 31.25 Hz)
        frame_rate = 31.25
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values, 
                           frame_rate=frame_rate)
        
        # Assert
        assert len(notes) == 1, f"Expected 1 note, got {len(notes)}"
        note = notes[0]
        
        assert note['pitch'] == 60, f"Expected pitch 60, got {note['pitch']}"
        assert abs(note['start'] - (5.0 / frame_rate)) < 1e-6, f"Wrong onset: {note['start']}"
        assert abs(note['end'] - (15.0 / frame_rate)) < 1e-6, f"Wrong offset: {note['end']}"
        assert abs(note['velocity'] - 0.8) < 1e-6, f"Wrong velocity: {note['velocity']}"
        
        # Export for C# testing
        expected = [{
            'pitch': 60,
            'start': 5.0 / frame_rate,
            'end': 15.0 / frame_rate,
            'velocity': 0.8
        }]
        self.export_test_case('single_note', onset_probs, frame_probs, offset_probs, 
                             velocity_values, expected, frame_rate=frame_rate)
    
    def test_multiple_notes_same_key(self):
        """Test multiple notes on the same key."""
        num_frames = 50
        key_index = 39  # C4
        frame_rate = 31.25
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # First note: frames 5-14, velocity 0.6
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 0.6
        for i in range(5, 15):
            frame_probs[i, key_index] = 1.0
        
        # Second note: frames 20-29, velocity 0.9
        onset_probs[20, key_index] = 1.0
        velocity_values[20, key_index] = 0.9
        for i in range(20, 30):
            frame_probs[i, key_index] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate)
        
        # Assert
        assert len(notes) == 2, f"Expected 2 notes, got {len(notes)}"
        
        assert notes[0]['pitch'] == 60
        assert abs(notes[0]['velocity'] - 0.6) < 1e-6
        
        assert notes[1]['pitch'] == 60
        assert abs(notes[1]['velocity'] - 0.9) < 1e-6
        
        # Export
        expected = [
            {'pitch': 60, 'start': 5.0/frame_rate, 'end': 15.0/frame_rate, 'velocity': 0.6},
            {'pitch': 60, 'start': 20.0/frame_rate, 'end': 30.0/frame_rate, 'velocity': 0.9}
        ]
        self.export_test_case('multiple_notes_same_key', onset_probs, frame_probs, 
                             offset_probs, velocity_values, expected, frame_rate=frame_rate)
    
    def test_polyphonic_notes(self):
        """Test notes playing simultaneously on different keys."""
        num_frames = 30
        frame_rate = 31.25
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # C4 (key 39) active frames 5-14
        onset_probs[5, 39] = 1.0
        velocity_values[5, 39] = 0.7
        for i in range(5, 15):
            frame_probs[i, 39] = 1.0
        
        # E4 (key 43) active frames 7-16 (overlaps with C4)
        onset_probs[7, 43] = 1.0
        velocity_values[7, 43] = 0.75
        for i in range(7, 17):
            frame_probs[i, 43] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate)
        
        # Assert - should get 2 notes sorted by onset
        assert len(notes) == 2, f"Expected 2 notes, got {len(notes)}"
        assert notes[0]['pitch'] == 60  # C4
        assert notes[1]['pitch'] == 64  # E4
        assert notes[0]['start'] < notes[1]['start']  # C4 starts first
        
        # Export
        expected = [
            {'pitch': 60, 'start': 5.0/frame_rate, 'end': 15.0/frame_rate, 'velocity': 0.7},
            {'pitch': 64, 'start': 7.0/frame_rate, 'end': 17.0/frame_rate, 'velocity': 0.75}
        ]
        self.export_test_case('polyphonic_notes', onset_probs, frame_probs, offset_probs,
                             velocity_values, expected, frame_rate=frame_rate)
    
    def test_minimum_duration_filtering(self):
        """Test that notes shorter than min_duration_seconds are filtered."""
        num_frames = 30
        key_index = 39
        frame_rate = 100.0  # 10ms per frame
        min_duration_seconds = 0.05  # 50ms = 5 frames minimum
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # Short note: 3 frames (30ms, below minimum)
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 0.7
        for i in range(5, 8):
            frame_probs[i, key_index] = 1.0
        
        # Long note: 10 frames (100ms, above minimum)
        onset_probs[15, key_index] = 1.0
        velocity_values[15, key_index] = 0.8
        for i in range(15, 25):
            frame_probs[i, key_index] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate, min_duration_seconds=min_duration_seconds)
        
        # Assert - only the long note should be decoded
        assert len(notes) == 1, f"Expected 1 note (short filtered), got {len(notes)}"
        assert notes[0]['pitch'] == 60
        assert abs(notes[0]['start'] - (15.0 / frame_rate)) < 1e-6
        
        # Export
        expected = [
            {'pitch': 60, 'start': 15.0/frame_rate, 'end': 25.0/frame_rate, 'velocity': 0.8}
        ]
        self.export_test_case('minimum_duration_filtering', onset_probs, frame_probs,
                             offset_probs, velocity_values, expected, 
                             frame_rate=frame_rate,
                             thresholds={'onset_thresh': 0.5, 'frame_thresh': 0.5, 
                                       'offset_thresh': 0.5, 'min_duration_seconds': min_duration_seconds})
    
    def test_rearticulation(self):
        """Test that a new onset ends the previous note (re-articulation)."""
        num_frames = 40
        key_index = 39
        frame_rate = 31.25
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # First note: frames 5-19
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 0.6
        for i in range(5, 20):  # Frame 20 is still active
            frame_probs[i, key_index] = 1.0
        
        # New onset at frame 20 (while frame still active) -> ends previous, starts new
        onset_probs[20, key_index] = 1.0
        velocity_values[20, key_index] = 0.9
        for i in range(20, 30):
            frame_probs[i, key_index] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate)
        
        # Assert - should get 2 notes, first ends at frame 20
        assert len(notes) == 2, f"Expected 2 notes, got {len(notes)}"
        assert notes[0]['end'] == 20.0 / frame_rate
        assert notes[1]['start'] == 20.0 / frame_rate
        assert notes[0]['velocity'] < notes[1]['velocity']  # 0.6 < 0.9
        
        # Export
        expected = [
            {'pitch': 60, 'start': 5.0/frame_rate, 'end': 20.0/frame_rate, 'velocity': 0.6},
            {'pitch': 60, 'start': 20.0/frame_rate, 'end': 30.0/frame_rate, 'velocity': 0.9}
        ]
        self.export_test_case('rearticulation', onset_probs, frame_probs, offset_probs,
                             velocity_values, expected, frame_rate=frame_rate)
    
    def test_velocity_clamping(self):
        """Test that velocity is clamped to [0, 1]."""
        num_frames = 20
        key_index = 39
        frame_rate = 31.25
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # Note with velocity > 1.0 (should be clamped)
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 1.5  # Out of range
        for i in range(5, 15):
            frame_probs[i, key_index] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate)
        
        # Assert
        assert len(notes) == 1
        assert notes[0]['velocity'] == 1.0, f"Velocity not clamped: {notes[0]['velocity']}"
        
        # Export
        expected = [
            {'pitch': 60, 'start': 5.0/frame_rate, 'end': 15.0/frame_rate, 'velocity': 1.0}
        ]
        self.export_test_case('velocity_clamping', onset_probs, frame_probs, offset_probs,
                             velocity_values, expected, frame_rate=frame_rate)
    
    def test_zero_velocity_filtering(self):
        """Test that notes with zero velocity are filtered out."""
        num_frames = 30
        key_index = 39
        frame_rate = 31.25
        
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # Note with zero velocity (should be filtered)
        onset_probs[5, key_index] = 1.0
        velocity_values[5, key_index] = 0.0  # Zero velocity
        for i in range(5, 15):
            frame_probs[i, key_index] = 1.0
        
        # Valid note
        onset_probs[20, key_index] = 1.0
        velocity_values[20, key_index] = 0.8
        for i in range(20, 30):
            frame_probs[i, key_index] = 1.0
        
        # Decode
        notes = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                           frame_rate=frame_rate)
        
        # Assert - only the second note should be decoded
        assert len(notes) == 1, f"Expected 1 note, got {len(notes)}"
        assert notes[0]['start'] > 0.1  # Should be the second note
        
        # Export
        expected = [
            {'pitch': 60, 'start': 20.0/frame_rate, 'end': 30.0/frame_rate, 'velocity': 0.8}
        ]
        self.export_test_case('zero_velocity_filtering', onset_probs, frame_probs,
                             offset_probs, velocity_values, expected, frame_rate=frame_rate)
    
    def test_threshold_sensitivity(self):
        """Test that different thresholds produce different results."""
        num_frames = 30
        key_index = 39
        frame_rate = 31.25
        
        # Use soft predictions (not 0 or 1)
        onset_probs = np.zeros((num_frames, 88))
        frame_probs = np.zeros((num_frames, 88))
        offset_probs = np.zeros((num_frames, 88))
        velocity_values = np.zeros((num_frames, 88))
        
        # Soft onset probability (0.4, below default 0.5 threshold)
        onset_probs[5, key_index] = 0.4
        velocity_values[5, key_index] = 0.8
        for i in range(5, 15):
            frame_probs[i, key_index] = 0.8
        
        # With default threshold (0.5), should not detect
        notes_high = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                                 onset_thresh=0.5, frame_rate=frame_rate)
        assert len(notes_high) == 0, "Should not detect with high threshold"
        
        # With lower threshold (0.3), should detect
        notes_low = decode_notes(onset_probs, frame_probs, offset_probs, velocity_values,
                               onset_thresh=0.3, frame_rate=frame_rate)
        assert len(notes_low) == 1, "Should detect with low threshold"
        
        # Export both test cases
        self.export_test_case('threshold_sensitivity_high', onset_probs, frame_probs,
                             offset_probs, velocity_values, [],
                             frame_rate=frame_rate,
                             thresholds={'onset_thresh': 0.5, 'frame_thresh': 0.5, 'offset_thresh': 0.5})
        
        expected_low = [
            {'pitch': 60, 'start': 5.0/frame_rate, 'end': 15.0/frame_rate, 'velocity': 0.8}
        ]
        self.export_test_case('threshold_sensitivity_low', onset_probs, frame_probs,
                             offset_probs, velocity_values, expected_low,
                             frame_rate=frame_rate,
                             thresholds={'onset_thresh': 0.3, 'frame_thresh': 0.5, 'offset_thresh': 0.5})


if __name__ == '__main__':
    # Run all tests
    test_suite = TestDecoderEquivalence()
    
    print("Running Python decoder equivalence tests...")
    print()
    
    test_suite.test_single_note()
    print("✓ single_note")
    
    test_suite.test_multiple_notes_same_key()
    print("✓ multiple_notes_same_key")
    
    test_suite.test_polyphonic_notes()
    print("✓ polyphonic_notes")
    
    test_suite.test_minimum_duration_filtering()
    print("✓ minimum_duration_filtering")
    
    test_suite.test_rearticulation()
    print("✓ rearticulation")
    
    test_suite.test_velocity_clamping()
    print("✓ velocity_clamping")
    
    test_suite.test_zero_velocity_filtering()
    print("✓ zero_velocity_filtering")
    
    test_suite.test_threshold_sensitivity()
    print("✓ threshold_sensitivity")
    
    print()
    print("All tests passed!")
    print(f"Test data exported to: {TestDecoderEquivalence.create_test_data_dir()}")
