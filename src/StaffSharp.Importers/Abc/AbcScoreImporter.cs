namespace StaffSharp.Importers.Abc;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Score importer for ABC notation format (v2.1).
/// </summary>
public sealed class AbcScoreImporter : IScoreImporter
{
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".abc" };

    public string FormatName => "ABC Notation";

    public async Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        // AbcParser.Parse is synchronous
        return AbcParser.Parse(content);
    }
}
