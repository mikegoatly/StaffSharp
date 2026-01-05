using System;
using System.Collections.Generic;
using System.Text;

namespace StaffSharp.Demo.Services;

/// <summary>
/// Platform-specific permission helper.
/// The platform projects will set the callbacks.
/// </summary>
public static class PermissionHelper
{
    public static Func<bool>? CheckRecordAudioPermission { get; set; }
    public static Action<Action<bool>>? RequestRecordAudioPermission { get; set; }
}
