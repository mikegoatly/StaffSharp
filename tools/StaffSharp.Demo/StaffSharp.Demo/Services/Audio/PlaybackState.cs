using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using PortAudioSharp;

using StaffSharp.Audio;

namespace StaffSharp.Demo.Services.Audio;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}
