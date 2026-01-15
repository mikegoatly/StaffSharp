import numpy as np

def decode_notes(onset_probs, frame_probs, offset_probs, velocity_values, 
                 onset_thresh=0.5, frame_thresh=0.5, offset_thresh=0.5, 
                 min_duration_seconds=0.05, frame_rate=100.0):
    """
    Python equivalent of StaffSharp.MachineLearning.ML.PostProcessing.NoteEventDecoder
    MUST be consistent with that C# implementation!
    
    Decodes frame-level predictions into discrete note events using state machine logic.
    Processes each piano key independently to handle polyphonic music.
    
    Args:
        onset_probs: (frames, 88) - Onset probability for each frame and pitch
        frame_probs: (frames, 88) - Frame activation probability (note is active)
        offset_probs: (frames, 88) - Offset probability (note is ending)
        velocity_values: (frames, 88) - Velocity prediction for each frame and pitch
        onset_thresh: Threshold for onset detection (default: 0.5) - matches C#
        frame_thresh: Threshold for frame activation (default: 0.5) - matches C#
        offset_thresh: Threshold for offset detection (default: 0.5) - matches C#
        min_duration_seconds: Minimum note duration in seconds (default: 0.05) - matches C# default
        frame_rate: Frame rate in Hz (default: 100.0 Hz based on hop_size=512, sr=44100)
    
    Returns:
        List of dicts with keys:
            - 'start': Onset time in seconds
            - 'end': Offset time in seconds
            - 'pitch': MIDI pitch (21-108)
            - 'velocity': Velocity in [0, 1]
    """
    notes = []
    num_frames, num_keys = onset_probs.shape
    
    # Calculate minimum note duration in frames
    min_duration_frames = min_duration_seconds * frame_rate
    
    # Process each piano key independently (88 keys, MIDI 21-108)
    for key_index in range(num_keys):
        midi_note = 21 + key_index  # MIDI note A0 is 21
        
        active_start_frame = None
        active_velocity = 0.0
        
        for frame_index in range(num_frames):
            is_onset = onset_probs[frame_index, key_index] >= onset_thresh
            is_offset = offset_probs[frame_index, key_index] >= offset_thresh
            is_active = frame_probs[frame_index, key_index] >= frame_thresh
            velocity = velocity_values[frame_index, key_index]
            
            # Case 1: New onset detected
            if is_onset:
                # If there's an active note, end it first (re-articulation)
                if active_start_frame is not None:
                    _try_create_note(notes, midi_note, active_start_frame, frame_index, 
                                    active_velocity, min_duration_frames, frame_rate)
                
                # Start new note
                active_start_frame = frame_index
                active_velocity = velocity
                
            # Case 2: Explicit offset detected or active note becomes inactive
            elif active_start_frame is not None and (is_offset or not is_active):
                _try_create_note(notes, midi_note, active_start_frame, frame_index,
                               active_velocity, min_duration_frames, frame_rate)
                active_start_frame = None
                active_velocity = 0.0
        
        # Handle note still active at end of audio
        if active_start_frame is not None:
            _try_create_note(notes, midi_note, active_start_frame, num_frames,
                           active_velocity, min_duration_frames, frame_rate)
             
    # Sort by onset time (stable sort to preserve insertion order for simultaneous notes)
    notes.sort(key=lambda x: x['start'])
    return notes


def _try_create_note(note_list, pitch, start_frame, end_frame, velocity, 
                     min_duration_frames, frame_rate):
    """Create a note event if it meets minimum duration and velocity requirements."""
    duration_frames = end_frame - start_frame
    duration_seconds = duration_frames / frame_rate
    
    # Filter out notes shorter than minimum duration (matches C# MinNoteLengthSeconds)
    if duration_seconds < (min_duration_frames / frame_rate):
        return
    
    # Filter out notes with zero velocity (likely false positives)
    if velocity <= 0.0:
        return
    
    # Convert frame indices to seconds
    start_seconds = start_frame / frame_rate
    end_seconds = end_frame / frame_rate
    
    # Clamp velocity to valid range [0, 1] (matches C# implementation)
    clamped_velocity = max(0.0, min(1.0, velocity))
    
    note_list.append({
        'start': start_seconds,
        'end': end_seconds,
        'pitch': pitch,
        'velocity': clamped_velocity
    })