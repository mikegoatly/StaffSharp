namespace StaffSharp.Audio.Pipeline.Stages;

/// <summary>
/// Pipeline stage that filters out onsets without detectable pitch.
/// This removes false positives from onset detection, particularly attack transients
/// that trigger onset detection but have no clear fundamental frequency.
/// After filtering, shifts remaining onsets so the first one starts at time 0.
/// </summary>
internal sealed class FilterUnpitchedOnsetsStage(AudioPipelineOptions options) : PipelineStageBase(options)
{
    protected override string StageName => "FilterUnpitchedOnsets";

    /// <summary>
    /// Filters out unpitched onsets and shifts remaining onsets to start at time 0.
    /// </summary>
    /// <param name="onsets">Onset times in seconds.</param>
    /// <param name="pitches">MIDI pitch numbers (-1 indicates unpitched/percussive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of filtered onsets and corresponding pitches.</returns>
    public Task<(double[] onsets, int[] pitches)> ExecuteAsync(
        double[] onsets,
        int[] pitches,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (onsets.Length != pitches.Length)
        {
            throw new ArgumentException("Onsets and pitches arrays must have the same length.");
        }

        ReportProgress("Filtering unpitched onsets");

        var filtered = new List<(double onset, int pitch)>();
        int unpitchedCount = 0;

        for (int i = 0; i < onsets.Length; i++)
        {
            // Keep pitched notes (MIDI >= 0), filter out unpitched (MIDI == -1)
            if (pitches[i] >= 0)
            {
                filtered.Add((onsets[i], pitches[i]));
            }
            else
            {
                unpitchedCount++;
            }
        }

        if (filtered.Count == 0)
        {
            EmitDiagnostics("Filtered onset count", 0);
            EmitDiagnostics("Unpitched onsets removed", unpitchedCount);
            return Task.FromResult((Array.Empty<double>(), Array.Empty<int>()));
        }

        // Shift onsets so the first one starts at time 0
        // This ensures musical content starts at beat 0 without leading rests
        var firstOnsetTime = filtered[0].onset;
        var shiftedOnsets = filtered.Select(x => x.onset - firstOnsetTime).ToArray();
        var filteredPitches = filtered.Select(x => x.pitch).ToArray();

        EmitDiagnostics("Filtered onset count", shiftedOnsets.Length);
        EmitDiagnostics("Unpitched onsets removed", unpitchedCount);
        EmitDiagnostics("Time shift applied (seconds)", firstOnsetTime);
        EmitDiagnostics("Filtered onsets", shiftedOnsets);
        EmitDiagnostics("Filtered pitches (MIDI)", filteredPitches);

        return Task.FromResult((shiftedOnsets, filteredPitches));
    }
}
