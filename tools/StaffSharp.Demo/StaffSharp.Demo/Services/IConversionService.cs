using StaffSharp.Audio;
using StaffSharp.Demo.ViewModels;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services
{
    internal interface IConversionService
    {
        Action<ImportProgress>? StatusChanged { get; set; }

        Task<ConversionResult> ConvertAsync(AudioBuffer audioBuffer, ProcessingOptions options, CancellationToken cancellationToken = default);
        Task<ConversionResult> ConvertAsync(string filePath, ProcessingOptions options, CancellationToken cancellationToken = default);
        Task ExportAsync(string fileName, NotationScore score, ProcessingOptions options, CancellationToken cancellationToken = default);
    }
}