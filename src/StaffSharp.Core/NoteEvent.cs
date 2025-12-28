using System;
using System.Collections.Generic;
using System.Text;

namespace StaffSharp.Core;

/// <summary>
/// Represents a musical note with timing and dynamics.
/// </summary>
public record NoteEvent(
    MidiNote Pitch,       // MIDI note number (supports microtones)
    TimeSpan Onset,       // When the note starts
    TimeSpan Duration,    // How long the note lasts
    Velocity Velocity     // Loudness (0.0 - 1.0)
)
{
    /// <summary>
    /// Gets the offset (end time) of the note.
    /// </summary>
    public TimeSpan Offset => Onset + Duration;
}
