using System.Diagnostics.CodeAnalysis;

namespace StaffSharp.Demo.Services.Audio;

public static class AudioService
{
    [DisallowNull]
    public static IAudioService Instance { get; set; } = null!;
}
