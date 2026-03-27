#if ANDROID
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content;
using Avalonia.Android;

namespace Avalonia.Labs.Notifications.Android;

internal static class PlatformSupport
{
    private static int s_lastRequestCode = 20000;

    public static int GetNextRequestCode() => s_lastRequestCode++;

    public static async Task<bool> CheckPermission(this Context context, string permission)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return true;
        }

        if (ContextCompat.CheckSelfPermission(context, permission) == Permission.Granted)
        {
            return true;
        }

        // RequestPermissions requires an Activity. Try to find one.
        if (context is not Activity activity)
        {
            activity = NativeNotificationManager.CurrentActivity;
        }

        if (activity is null || activity is not IActivityResultHandler mainActivity)
        {
            return false;
        }

        var currentRequestCode = GetNextRequestCode();
        var tcs = new TaskCompletionSource<bool>();
        mainActivity.RequestPermissionsResult += RequestPermissionsResult;
        activity.RequestPermissions(new[] { permission }, currentRequestCode);

        return await tcs.Task;

        void RequestPermissionsResult(int requestCode, string[] arg2, Permission[] arg3)
        {
            if (currentRequestCode != requestCode)
            {
                return;
            }

            mainActivity.RequestPermissionsResult -= RequestPermissionsResult;

            _ = tcs.TrySetResult(arg3.All(p => p == Permission.Granted));
        }
    }
}
#endif
