using StaffSharp.Audio.Diagnostics;

namespace StaffSharp.Audio.Pipeline;

/// <summary>
/// Context passed through the audio processing pipeline.
/// Contains shared options and optional diagnostics collector.
/// </summary>
public sealed class PipelineContext
{
    public PipelineContext(
        IDiagnosticsCollector? diagnosticsCollector = null,
        CancellationToken cancellationToken = default)
    {
        DiagnosticsCollector = diagnosticsCollector;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Optional diagnostics collector. Null when diagnostics disabled (zero-overhead).
    /// </summary>
    public IDiagnosticsCollector? DiagnosticsCollector { get; }

    /// <summary>
    /// Cancellation token for long-running operations.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Emits diagnostic data if diagnostics are enabled.
    /// Uses lazy evaluation to avoid computation when diagnostics disabled.
    /// </summary>
    /// <typeparam name="T">Type of diagnostic data.</typeparam>
    /// <param name="stageName">Name of the pipeline stage.</param>
    /// <param name="key">Diagnostic data key.</param>
    /// <param name="valueFactory">Factory function that produces the diagnostic value (only called if diagnostics enabled).</param>
    public void EmitDiagnostics<T>(string stageName, string key, Func<T> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        // Zero-overhead pattern: null check optimized by branch predictor
        // Func<T> prevents value computation when diagnostics disabled
        if (DiagnosticsCollector != null)
        {
            DiagnosticsCollector.Collect(stageName, key, valueFactory());
        }
    }

    /// <summary>
    /// Emits diagnostic data if diagnostics are enabled (non-lazy version for pre-computed values).
    /// </summary>
    public void EmitDiagnostics<T>(string stageName, string key, T value)
    {
        DiagnosticsCollector?.Collect(stageName, key, value);
    }
}
