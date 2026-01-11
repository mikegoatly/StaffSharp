using System.Text;

using StaffSharp.Notation;

namespace StaffSharp
{
    public static class ScoreExporterExtensions
    {
        /// <summary>
        /// Exports the given <see cref="NotationScore"/> to a string using the specified <see cref="IScoreExporter"/>.
        /// Note: This method assumes that the exported format is text-based - trying to use it with a binary format, e.g. MIDI will
        /// produce incorrect results.
        /// </summary>
        /// <param name="exporter"></param>
        /// <param name="score"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ExportToStringAsync(
            this IScoreExporter exporter,
            NotationScore score,
            IReadOnlyDictionary<string, string>? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(exporter);

            using var memoryStream = new MemoryStream();
            await exporter.ExportAsync(score, memoryStream, options, cancellationToken).ConfigureAwait(false);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
