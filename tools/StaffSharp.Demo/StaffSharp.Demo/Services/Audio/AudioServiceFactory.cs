namespace StaffSharp.Demo.Services.Audio;

// <summary>
/// Factory for creating platform-specific audio service implementations.
/// </summary>
public static class AudioServiceFactory
{
    /// <summary>
    /// Creates the appropriate audio service for the current platform.
    /// </summary>
    public static IAudioService Create()
    {
#if ANDROID
        return new AndroidAudioService();
#else
        return new AudioService();
#endif
    }
}
