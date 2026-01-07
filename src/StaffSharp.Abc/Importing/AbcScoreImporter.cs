using StaffSharp.Abc.Importing;

namespace StaffSharp.Abc.Importing;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Score importer for ABC notation format (v2.1).
/// </summary>
public sealed class AbcScoreImporter : IScoreImporter
{
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".abc" };

    public string FormatName => "ABC Notation";

    public async Task<NotationScore> ImportAsync(Stream stream, IProgress<ImportProgress>? progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        progress?.Report(new ImportProgress("ABC Import", "Reading ABC file"));
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        progress?.Report(new ImportProgress("ABC Import", "Parsing ABC notation"));
        // AbcParser.Parse is synchronous
        var output = AbcParser.Parse(content);

        progress?.Report(new ImportProgress("ABC Import", "Import complete"));
        return output;
    }

    public Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return ImportAsync(stream, progress: null, cancellationToken);
    }
}
