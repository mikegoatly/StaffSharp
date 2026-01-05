
using StaffSharp.Demo.ViewModels;

namespace StaffSharp.Demo.Services
{
    public interface IConversionService
    {
        Task<ConversionResult> ConvertAbcAsync(string abcContent, CancellationToken cancellationToken = default);
        Task<ConversionResult> ConvertAudioAsync(string filePath, ProcessingOptions options, CancellationToken cancellationToken = default);
    }
}