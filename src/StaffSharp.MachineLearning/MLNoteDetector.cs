using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;
using StaffSharp.Performance;
using StaffSharp.Quantization;

namespace StaffSharp.MachineLearning;

/// <summary>
/// ML-based polyphonic note detector using trained neural network models.
/// Transcribes polyphonic audio (e.g., piano) to note events with onsets, offsets, pitches, and velocities.
/// </summary>
public sealed class MLNoteDetector : INoteDetector
{
    private readonly OnnxTranscriber _transcriber;
    private readonly SimpleTimeSignatureDetector _timeSignatureDetector;
    private readonly ITempoDetector _tempoDetector;
    private readonly PolyphonicQuantizer _quantizer;
    private readonly NoteEventDecoder _decoder;
    private readonly HarmonicSuppressor _harmonicSuppressor;
    private readonly MLTranscriptionOptions _options;

    /// <summary>
    /// Creates a new ML-based note detector with the specified options.
    /// If options is null, uses default settings with CombFilterTempoDetector.
    /// </summary>
    /// <param name="options">ML transcription options (model path, thresholds, etc.). If null, uses defaults.</param>
    public MLNoteDetector(MLTranscriptionOptions? options = null)
    {
        _options = options ?? new MLTranscriptionOptions();

#pragma warning disable CA2000 // Dispose objects before losing scope - Disposed by MLNoteDetector
        _transcriber = new OnnxTranscriber(_options);
#pragma warning restore CA2000 // Dispose objects before losing scope
        _timeSignatureDetector = new SimpleTimeSignatureDetector();
        _tempoDetector = TempoDetectorFactory.Create(_options.TempoOptions);
        _quantizer = new PolyphonicQuantizer();
        _decoder = new NoteEventDecoder(_options);
        _harmonicSuppressor = new HarmonicSuppressor(_options.HarmonicSuppressionOptions);
    }

    /// <summary>
    /// Detects and quantizes notes from audio using ML-based polyphonic transcription.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quantization result containing quantized notes and tempo map.</returns>
    public async Task<PerformanceTimeline> DetectAsync(PipelineProgress progress, AudioBuffer audio, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(audio);
        ct.ThrowIfCancellationRequested();

        progress.ReportProgress("Transcribing audio with ML model");

        // ONNX inference (audio → piano roll)
        var transcriptionResult = await _transcriber.TranscribeAsync(progress, audio).ConfigureAwait(false);

        progress.EmitDiagnostics("Frame count", transcriptionResult.NumFrames);
        progress.EmitDiagnostics("Duration (seconds)", transcriptionResult.DurationSeconds);
        progress.EmitDiagnostics("Frame rate (Hz)", transcriptionResult.FrameRate);

        progress.ReportProgress("Decoding note events from predictions");

        // Decode piano roll to note events
        var noteEvents = _decoder.Decode(transcriptionResult);

        // Shift note events to remove leading silence
        // (Align first note to beat 0, like the monophonic detector does)
        if (noteEvents.Count > 0)
        {
            var firstNoteOnset = noteEvents[0].Onset;
            if (firstNoteOnset > TimeSpan.Zero)
            {
                noteEvents = ShiftNoteEvents(noteEvents, -firstNoteOnset);
                progress.EmitDiagnostics("Leading silence trimmed", firstNoteOnset);
            }
        }

        progress.EmitDiagnostics("Note count", noteEvents.Count);

        // Emit detailed note information for debugging
        if (progress.DiagnosticsEnabled && noteEvents.Count > 0)
        {
            var noteSummary = noteEvents.Select((n, i) =>
                $"[{i}] MIDI {n.Pitch.MidiNumber} @ {n.Onset.TotalSeconds:F3}s, dur={n.Duration.TotalSeconds:F3}s, vel={n.Velocity.Value:F2}"
            ).ToArray();
            progress.EmitDiagnostics("NotePitches", noteSummary);
        }

        if (noteEvents.Count == 0)
        {
            // No notes detected - return empty result with default tempo
            return PerformanceTimeline.Empty;
        }

        // Apply harmonic suppression BEFORE tempo/time signature detection
        // This ensures we detect the correct meter based on the actual notes (without harmonics)
        var beforeCount = noteEvents.Count;
        progress.EmitDiagnostics("Harmonic suppression enabled", _options.HarmonicSuppressionOptions.SuppressHarmonics);
        noteEvents = _harmonicSuppressor.SuppressHarmonics(noteEvents);
        var afterCount = noteEvents.Count;

        if (progress.DiagnosticsEnabled && beforeCount != afterCount)
        {
            progress.EmitDiagnostics("Harmonics suppressed", $"{beforeCount - afterCount} notes removed ({beforeCount} → {afterCount})");

            // Emit filtered notes for debugging
            var filteredSummary = noteEvents.Select((n, i) =>
                $"[{i}] MIDI {n.Pitch.MidiNumber} @ {n.Onset.TotalSeconds:F3}s, dur={n.Duration.TotalSeconds:F3}s, vel={n.Velocity.Value:F2}"
            ).ToArray();

            progress.EmitDiagnostics("FilteredNotes", filteredSummary);
        }

        progress.ReportProgress("Analyzing tempo and time signature");


        progress.ReportProgress("Analyzing tempo and time signature");

        // Extract onset times for tempo/time signature detection
        var onsetTimes = noteEvents.Select(n => n.Onset.TotalSeconds).ToArray();
        var timeSignatures = _timeSignatureDetector.DetectTimeSignatures(progress, onsetTimes);

        // Validate time signatures
        if (timeSignatures == null || timeSignatures.Count == 0)
        {
            timeSignatures = [new TimeSignatureChange(Rational.Zero, Notation.TimeSignature.CommonTime)];
        }

        // Detect tempo
        var tempoChanges = _tempoDetector.DetectTempo(progress, onsetTimes);

        if (tempoChanges == null || tempoChanges.Count == 0)
        {
            throw new InvalidOperationException("Tempo detection failed - detector returned null or empty tempo changes.");
        }

        var detectedTempo = tempoChanges[0].BeatsPerMinute;
        progress.EmitDiagnostics("Estimated tempo (BPM)", detectedTempo);
        progress.EmitDiagnostics("Time signatures", timeSignatures.Count);

        // Combine detected tempo and time signatures into a single TempoMap
        var finalTempoMap = new TempoMap(tempoChanges, timeSignatures);

        progress.ReportProgress("Quantizing note events");

        // Quantize note events (snap to rhythmic grid)
        var (quantizedNotes, refinedTempoMap) = _quantizer.Quantize(noteEvents, finalTempoMap);

        progress.EmitDiagnostics("Quantized note count", quantizedNotes.Count);

        // Emit quantization details for debugging alignment issues
        if (progress.DiagnosticsEnabled && quantizedNotes.Count > 0)
        {
            var quantizedSummary = quantizedNotes.Select((n, i) =>
                $"[{i}] MIDI {n.Pitch.MidiNumber} @ beat {n.OnsetBeats.ToDouble():F3}, dur={n.DurationBeats.ToDouble():F3} beats (error: {n.QuantizationMetadata.OnsetError.TotalSeconds:F4}s)"
            ).ToArray();

            progress.EmitDiagnostics("QuantizedNotes", quantizedSummary);
        }

        if (_options.TreatPolyphonyAsChords)
        {
            foreach (var note in quantizedNotes)
            {
                note.VoiceHint = 1;
            }
        }

        return new PerformanceTimeline(refinedTempoMap, quantizedNotes);
    }

    /// <summary>
    /// Shifts all note event onsets by the specified time delta.
    /// Used to trim leading silence and align notes to beat 0.
    /// </summary>
    private static IReadOnlyList<NoteEvent> ShiftNoteEvents(IReadOnlyList<NoteEvent> events, TimeSpan delta)
    {
        if (events.Count == 0 || delta == TimeSpan.Zero)
        {
            return events;
        }

        var shifted = new List<NoteEvent>(events.Count);
        foreach (var evt in events)
        {
            var newOnset = evt.Onset + delta;

            // Skip notes that would have negative onset (shouldn't happen, but be safe)
            if (newOnset < TimeSpan.Zero)
            {
                continue;
            }

            shifted.Add(evt with { Onset = newOnset });
        }

        return shifted;
    }

    /// <summary>
    /// Disposes the underlying transcriber if it implements IDisposable.
    /// </summary>
    public void Dispose()
    {
        _transcriber.Dispose();
    }
}
