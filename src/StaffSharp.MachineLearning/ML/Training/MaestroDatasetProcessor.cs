namespace StaffSharp.MachineLearning.ML.Training;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using StaffSharp.Audio;
using StaffSharp.Audio.IO;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Features;

/// <summary>
/// Processes individual files from the MAESTRO dataset into training samples.
/// </summary>
public sealed class MaestroDatasetProcessor
{
    private const int MinMidiNote = 21;  // A0
    private const int MaxMidiNote = 108; // C8
    private const int NumKeys = 88;
    private const int HopSize = 512;
    private const int SampleRate = 16000;

    private readonly MelSpectrogramExtractor _extractor;

    public MaestroDatasetProcessor()
    {
        _extractor = new MelSpectrogramExtractor();
    }

    /// <summary>
    /// Processes a single audio/MIDI pair into a training data sample.
    /// </summary>
    /// <param name="audioPath">Path to audio file (.wav).</param>
    /// <param name="midiPath">Path to MIDI file (.mid or .midi).</param>
    /// <returns>Training data sample with features and labels.</returns>
    public async Task<TrainingDataSample> ProcessFileAsync(string audioPath, string midiPath)
    {
        ArgumentNullException.ThrowIfNull(audioPath);
        ArgumentNullException.ThrowIfNull(midiPath);

        // 1. Load and process audio
        AudioBuffer audio;
        using (var stream = File.OpenRead(audioPath))
        {
            audio = await WavReader.ReadAsync(stream).ConfigureAwait(false);
        }

        // 2. Extract mel spectrogram features
        var melSpec = _extractor.ExtractFeatures(PipelineProgress.Null, audio);

        var numFrames = melSpec.GetLength(0);
        var frameRate = (float)SampleRate / HopSize;

        // 3. Parse MIDI and generate ground truth
        var notes = ParseMidiFile(midiPath);
        var (pianoRoll, onsetRoll, offsetRoll, velocityRoll) = NotesToRolls(notes, numFrames, frameRate);

        return new TrainingDataSample
        {
            MelSpectrogram = melSpec,
            PianoRoll = pianoRoll,
            OnsetRoll = onsetRoll,
            OffsetRoll = offsetRoll,
            VelocityRoll = velocityRoll,
            AudioPath = audioPath,
            MidiPath = midiPath
        };
    }

    private static List<NoteEvent> ParseMidiFile(string path)
    {
        var midiFile = MidiFile.Read(path);
        var notes = new List<NoteEvent>();

        // Use DryWetMidi's TempoMap for accurate timing
        var tempoMap = midiFile.GetTempoMap();

        // Get all note events
        var noteEvents = midiFile.GetNotes();

        foreach (var note in noteEvents)
        {
            if (note.NoteNumber >= MinMidiNote && note.NoteNumber <= MaxMidiNote)
            {
                var onsetTime = note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                var offsetTime = note.EndTimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;

                notes.Add(new NoteEvent
                {
                    Onset = onsetTime,
                    Offset = offsetTime,
                    Pitch = note.NoteNumber,
                    Velocity = note.Velocity / 127.0 // Normalize to [0, 1]
                });
            }
        }

        return notes;
    }

    private static (float[,] piano, float[,] onset, float[,] offset, float[,] velocity) NotesToRolls(
        List<NoteEvent> notes,
        int numFrames,
        float frameRate)
    {
        var pianoRoll = new float[numFrames, NumKeys];
        var onsetRoll = new float[numFrames, NumKeys];
        var offsetRoll = new float[numFrames, NumKeys];
        var velocityRoll = new float[numFrames, NumKeys];

        foreach (var note in notes)
        {
            var keyIndex = note.Pitch - MinMidiNote;

            // Convert times to frame indices
            var onsetFrame = (int)Math.Round(note.Onset * frameRate);
            var offsetFrame = (int)Math.Round(note.Offset * frameRate);

            // Clamp to valid range
            onsetFrame = Math.Clamp(onsetFrame, 0, numFrames - 1);
            offsetFrame = Math.Clamp(offsetFrame, 0, numFrames - 1);

            // Mark onset
            if (onsetFrame < numFrames)
            {
                onsetRoll[onsetFrame, keyIndex] = 1.0f;
                velocityRoll[onsetFrame, keyIndex] = (float)note.Velocity;
            }

            // Mark offset
            if (offsetFrame < numFrames && offsetFrame != onsetFrame)
            {
                offsetRoll[offsetFrame, keyIndex] = 1.0f;
            }

            // Mark active frames
            for (int frame = onsetFrame; frame <= Math.Min(offsetFrame, numFrames - 1); frame++)
            {
                pianoRoll[frame, keyIndex] = 1.0f;
            }
        }

        return (pianoRoll, onsetRoll, offsetRoll, velocityRoll);
    }

    private sealed class NoteEvent
    {
        public double Onset { get; init; }
        public double Offset { get; init; }
        public int Pitch { get; init; }
        public double Velocity { get; init; }
    }
}
