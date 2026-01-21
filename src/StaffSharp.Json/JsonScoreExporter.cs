namespace StaffSharp.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Score exporter for JSON format.
/// </summary>
public sealed class JsonScoreExporter : IScoreExporter
{
    private static readonly JsonSerializerOptions _indentedSerializerOptions = CreateOptions(indented: true);
    private static readonly JsonSerializerOptions _unformattedSerializerOptions = CreateOptions(indented: false);

    private static JsonSerializerOptions CreateOptions(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = NotationScoreSerializationContext.Default
                .WithAddedModifier(JsonSerializerConfig.ConfigureContext)
        };

        return options;
    }

    public IReadOnlyList<string> SupportedExtensions { get; } = [".json"];

    public string FormatName => "JSON";

    public IReadOnlyList<ExportOption> AvailableOptions { get; } =
    [
        new ExportOption(
            "indent",
            "Enable pretty-printed JSON with indentation. Format: \"true\" or \"false\". (JSON only)",
            "true")
    ];

    public async Task ExportAsync(
        NotationScore score,
        Stream stream,
        IReadOnlyDictionary<string, string>? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(stream);

        // Parse options
        var writeIndented = true; // default to pretty-printed
        if (options != null && options.TryGetValue("indent", out var indentValue))
        {
            if (bool.TryParse(indentValue, out var indent))
            {
                writeIndented = indent;
            }
        }

        var serializationOptions = writeIndented
            ? _indentedSerializerOptions
            : _unformattedSerializerOptions;

        // Serialize to stream
        await JsonSerializer.SerializeAsync(stream, score, serializationOptions, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Source-generated JSON serialization context for NotationScore.
/// Uses NotationPolymorphicTypeResolver for polymorphism configuration.
/// All types serialize automatically - no custom converters needed.
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(NotationScore))]
[JsonSerializable(typeof(NotationNote))]
[JsonSerializable(typeof(Chord))]
[JsonSerializable(typeof(Rest))]
internal sealed partial class NotationScoreSerializationContext : JsonSerializerContext;
