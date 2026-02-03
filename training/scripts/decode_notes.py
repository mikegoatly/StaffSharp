import numpy as np


def decode_notes(onset_probs, frame_probs, offset_probs, velocity_values, 
                 onset_thresh=0.5, frame_thresh=0.5, offset_thresh=0.5, 
                 min_duration_seconds=0.05, frame_rate=100.0,
                 gap_tolerance_seconds=0.05, min_velocity=0.1):
    """
    Python equivalent of StaffSharp.MachineLearning.ML.PostProcessing.NoteEventDecoder
    MUST be consistent with that C# implementation!
    
    Decodes frame-level predictions into discrete note events using state machine logic.
    Includes gap-filling (hysteresis) for bass notes and velocity filtering.
    
    Args:
        onset_probs: (frames, 88) - Onset probability
        frame_probs: (frames, 88) - Frame activation probability
        offset_probs: (frames, 88) - Offset probability
        velocity_values: (frames, 88) - Velocity prediction
        onset_thresh: Threshold for onset detection (default: 0.5)
        frame_thresh: Threshold for frame activation (default: 0.5)
        offset_thresh: Threshold for offset detection (default: 0.5)
        min_duration_seconds: Minimum note duration (default: 0.05)
        frame_rate: Frame rate in Hz
        gap_tolerance_seconds: Time to wait before killing a note that lost frame activation (default: 0.05)
        min_velocity: Minimum velocity (0.0-1.0) to register a note (default: 0.1)
    
    Returns:
        List of dicts with keys: 'start', 'end', 'pitch', 'velocity'
    """
    notes = []
    num_frames, num_keys = onset_probs.shape
    
    # Calculate thresholds in frames
    min_duration_frames = min_duration_seconds * frame_rate
    gap_tolerance_frames = int(np.ceil(gap_tolerance_seconds * frame_rate))
    
    # Process each piano key independently (88 keys, MIDI 21-108)
    for key_index in range(num_keys):
        midi_note = 21 + key_index
        
        active_start_frame = None
        active_velocity = 0.0
        gap_frame_count = 0  # How many frames have we been "missing" the note?
        
        for frame_index in range(num_frames):
            onset_p = onset_probs[frame_index, key_index]
            offset_p = offset_probs[frame_index, key_index]
            frame_p = frame_probs[frame_index, key_index]
            velocity = velocity_values[frame_index, key_index]
            
            is_onset = onset_p >= onset_thresh
            is_offset = offset_p >= offset_thresh
            is_active = frame_p >= frame_thresh
            
            # 1. Check for New Onset
            # We enforce a "Consensus" check: Frame prob shouldn't be zero.
            if is_onset and is_active:
                
                # Ghost busting: If velocity is too low, ignore this onset entirely
                if velocity < min_velocity:
                    continue
                
                # If there's an active note, end it first (re-articulation)
                if active_start_frame is not None:
                    # Trim any trailing silence if we were in a gap
                    prev_end_frame = frame_index - gap_frame_count
                    _try_create_note(notes, midi_note, active_start_frame, prev_end_frame, 
                                     active_velocity, min_duration_frames, frame_rate)
                
                # Start new note
                active_start_frame = frame_index
                active_velocity = velocity
                gap_frame_count = 0
                
            # 2. Handle Active Note Logic
            elif active_start_frame is not None:
                explicit_stop = is_offset
                signal_lost = not is_active
                
                if explicit_stop:
                    # Explicit offset detected - kill immediately
                    _try_create_note(notes, midi_note, active_start_frame, frame_index,
                                     active_velocity, min_duration_frames, frame_rate)
                    active_start_frame = None
                    active_velocity = 0.0
                    gap_frame_count = 0
                    
                elif signal_lost:
                    # Frame signal died, but no explicit offset. Start counting the gap.
                    gap_frame_count += 1
                    
                    # If gap is too long, confirm the kill
                    if gap_frame_count > gap_tolerance_frames:
                        # The note actually ended 'gap_tolerance' frames ago
                        actual_end_frame = frame_index - gap_tolerance_frames
                        _try_create_note(notes, midi_note, active_start_frame, actual_end_frame,
                                         active_velocity, min_duration_frames, frame_rate)
                        
                        active_start_frame = None
                        active_velocity = 0.0
                        gap_frame_count = 0
                else:
                    # Signal is alive! Bridge the gap.
                    gap_frame_count = 0
        
        # Handle note still active at end of audio
        if active_start_frame is not None:
            # If we ended while in a gap, trim the gap
            final_frame = num_frames - gap_frame_count
            _try_create_note(notes, midi_note, active_start_frame, final_frame,
                             active_velocity, min_duration_frames, frame_rate)
             
    # Sort by onset time
    notes.sort(key=lambda x: x['start'])
    return notes


def _try_create_note(note_list, pitch, start_frame, end_frame, velocity, 
                     min_duration_frames, frame_rate):
    """Create a note event if it meets minimum duration requirements."""
    
    # Sanity check
    if end_frame <= start_frame:
        return

    duration_frames = end_frame - start_frame
    duration_seconds = duration_frames / frame_rate
    
    # Filter out notes shorter than minimum duration
    if duration_seconds < (min_duration_frames / frame_rate):
        return
    
    # Note: Min velocity check is now done at onset detection,
    # but we keep this sanity check just in case logic changes.
    if velocity <= 0.0:
        return
    
    start_seconds = start_frame / frame_rate
    end_seconds = end_frame / frame_rate
    
    clamped_velocity = max(0.0, min(1.0, velocity))
    
    note_list.append({
        'start': start_seconds,
        'end': end_seconds,
        'pitch': pitch,
        'velocity': clamped_velocity
    })