namespace StaffSharp.Validation;

using StaffSharp.Notation;

/// <summary>
/// Validates a NotationScore for common errors and inconsistencies.
/// </summary>
public sealed class NotationScoreValidator
{
    /// <summary>
    /// Validates a notation score and returns any issues found.
    /// </summary>
    /// <param name="score">The score to validate.</param>
    /// <returns>Validation results.</returns>
    public static ValidationResult Validate(NotationScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate metadata
        ValidateMetadata(score.Metadata, errors, warnings);

        // Validate parts and their structure
        if (score.Parts.Count == 0)
        {
            errors.Add("Score has no parts");
        }

        foreach (var part in score.Parts)
        {
            ValidatePart(part, score.Metadata.TimeSignature, errors, warnings);
        }

        return new ValidationResult(errors, warnings);
    }

    private static void ValidateMetadata(ScoreMetadata metadata, List<string> errors, List<string> warnings)
    {
        // Validate tempo
        if (metadata.Tempo < 20)
        {
            errors.Add($"Tempo is too slow: {metadata.Tempo} BPM (minimum 20 BPM)");
        }
        else if (metadata.Tempo > 300)
        {
            errors.Add($"Tempo is too fast: {metadata.Tempo} BPM (maximum 300 BPM)");
        }
        else if (metadata.Tempo < 40)
        {
            warnings.Add($"Unusually slow tempo: {metadata.Tempo} BPM");
        }
        else if (metadata.Tempo > 240)
        {
            warnings.Add($"Unusually fast tempo: {metadata.Tempo} BPM");
        }

        // Validate time signature
        if (metadata.TimeSignature.Numerator < 1 || metadata.TimeSignature.Numerator > 32)
        {
            errors.Add($"Invalid time signature numerator: {metadata.TimeSignature.Numerator}");
        }

        if (metadata.TimeSignature.Denominator < 1 || metadata.TimeSignature.Denominator > 64)
        {
            errors.Add($"Invalid time signature denominator: {metadata.TimeSignature.Denominator}");
        }
    }

    private static void ValidatePart(Part part, TimeSignature defaultTimeSignature, List<string> errors, List<string> warnings)
    {
        if (part.Voices.Count == 0)
        {
            warnings.Add($"Part '{part.Name}' has no voices");
            return;
        }

        foreach (var voice in part.Voices)
        {
            ValidateVoice(voice, defaultTimeSignature, part.Name, errors, warnings);
        }
    }

    private static void ValidateVoice(Voice voice, TimeSignature defaultTimeSignature, string partName, List<string> errors, List<string> warnings)
    {
        if (voice.Measures.Count == 0)
        {
            warnings.Add($"Part '{partName}', Voice {voice.Number} has no measures");
            return;
        }

        for (int i = 0; i < voice.Measures.Count; i++)
        {
            var isLastMeasure = i == voice.Measures.Count - 1;
            ValidateMeasure(voice.Measures[i], defaultTimeSignature, partName, voice.Number, isLastMeasure, errors, warnings);
        }
    }

    private static void ValidateMeasure(
        Measure measure,
        TimeSignature defaultTimeSignature,
        string partName,
        int voiceNumber,
        bool isLastMeasure,
        List<string> errors,
        List<string> warnings)
    {
        var timeSignature = measure.TimeSignature ?? defaultTimeSignature;
        // Expected duration in beats: numerator (e.g., 4 for 4/4 time = 4 beats)
        var expectedDuration = Rational.Create(timeSignature.Numerator, 1);

        // Calculate actual duration
        var actualDuration = Rational.Zero;
        foreach (var evt in measure.Events)
        {
            actualDuration += evt.Duration.ToBeats();
        }

        // Allow small floating-point tolerance
        var diff = actualDuration.ToDouble() - expectedDuration.ToDouble();
        if (Math.Abs(diff) > 0.001)
        {
            // Allow partial final measures (pickup/incomplete ending measures are common)
            if (isLastMeasure && actualDuration < expectedDuration)
            {
                // This is OK - partial final measure
                warnings.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measure.Number}: Incomplete final measure. Expected {expectedDuration} beats, got {actualDuration} beats");
            }
            else
            {
                errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measure.Number}: Duration mismatch. Expected {expectedDuration}, got {actualDuration}");
            }
        }

        // Validate individual events
        foreach (var evt in measure.Events)
        {
            ValidateEvent(evt, partName, voiceNumber, measure.Number, errors, warnings);
        }
    }

    private static void ValidateEvent(
        INotationEvent evt,
        string partName,
        int voiceNumber,
        int measureNumber,
        List<string> errors,
        List<string> warnings)
    {
        switch (evt)
        {
            case NotationNote note:
                ValidateNote(note, partName, voiceNumber, measureNumber, errors, warnings);
                break;

            case Chord chord:
                ValidateChord(chord, partName, voiceNumber, measureNumber, errors);
                break;

            case Rest rest:
                if (rest.Duration.ToBeats() <= Rational.Zero)
                {
                    errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Rest has zero or negative duration");
                }
                break;
        }
    }

    private static void ValidateNote(
        NotationNote note,
        string partName,
        int voiceNumber,
        int measureNumber,
        List<string> errors,
        List<string> warnings)
    {
        if (note.Duration.ToBeats() <= Rational.Zero)
        {
            errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Note has zero or negative duration");
        }

        // Validate MIDI note range (for eventual MIDI export)
        var midiNote = (note.Pitch.Octave * 12) + (int)note.Pitch.PitchClass;
        if (midiNote < 0 || midiNote > 127)
        {
            warnings.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Note {note.Pitch} is outside MIDI range (0-127)");
        }

        if (note.Velocity.Value < 0 || note.Velocity.Value > 1)
        {
            errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Invalid velocity {note.Velocity.Value} (must be 0.0-1.0)");
        }
    }

    private static void ValidateChord(
        Chord chord,
        string partName,
        int voiceNumber,
        int measureNumber,
        List<string> errors)
    {
        if (chord.Duration.ToBeats() <= Rational.Zero)
        {
            errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Chord has zero or negative duration");
        }

        // Note: Chord constructor already enforces minimum 2 pitches, so no validation needed here

        if (chord.Velocity.Value < 0 || chord.Velocity.Value > 1)
        {
            errors.Add($"Part '{partName}', Voice {voiceNumber}, Measure {measureNumber}: Invalid chord velocity {chord.Velocity.Value} (must be 0.0-1.0)");
        }
    }
}

/// <summary>
/// Results of notation score validation.
/// </summary>
/// <param name="Errors">Critical errors that prevent successful processing.</param>
/// <param name="Warnings">Non-critical issues that may indicate problems.</param>
public sealed record ValidationResult(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets whether the validation passed (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets whether there are any issues (errors or warnings).
    /// </summary>
    public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;
}
