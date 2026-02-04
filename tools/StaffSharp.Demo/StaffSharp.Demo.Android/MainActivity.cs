using Android.Content.PM;
using Android.OS;

using Avalonia;
using Avalonia.Android;

using StaffSharp.Demo.Services.Audio;

namespace StaffSharp.Demo.Android
{
    [Activity(
        Label = "StaffSharp.Demo.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        private const int RECORD_AUDIO_PERMISSION_REQUEST = 1;
        private static MainActivity? _instance;
        private static Action<bool>? _permissionCallback;

        public MainActivity()
        {
            Services.Audio.AudioService.Instance = new AndroidAudioService();
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _instance = this;

            // Set up permission callbacks for the shared code
            Services.PermissionHelper.CheckRecordAudioPermission = CheckRecordAudioPermission;
            Services.PermissionHelper.RequestRecordAudioPermission = RequestRecordAudioPermission;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _instance = null;
        }

        public static bool CheckRecordAudioPermission()
        {
            if (_instance == null) return false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                return _instance.CheckSelfPermission(global::Android.Manifest.Permission.RecordAudio) == Permission.Granted;
            }

            return true; // Pre-M versions grant permissions at install time
        }

        public static void RequestRecordAudioPermission(Action<bool> callback)
        {
            if (_instance == null)
            {
                callback(false);
                return;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                _permissionCallback = callback;
                _instance.RequestPermissions(new[] { global::Android.Manifest.Permission.RecordAudio }, RECORD_AUDIO_PERMISSION_REQUEST);
            }
            else
            {
                callback(true); // Pre-M versions grant permissions at install time
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RECORD_AUDIO_PERMISSION_REQUEST)
            {
                bool granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
                _permissionCallback?.Invoke(granted);
                _permissionCallback = null;
            }
        }
    }
}
