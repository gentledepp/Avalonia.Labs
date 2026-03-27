#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Avalonia.Labs.Notifications.Android
{
    internal class NativeNotificationManager : INativeNotificationManagerImpl, IDisposable
    {
        private readonly Dictionary<uint, INativeNotification> _notifications = new Dictionary<uint, INativeNotification>();
        private readonly Context _context;
        private bool _isActive;

        /// <summary>
        /// Set this to the current Activity so that permission requests can be made.
        /// Call <see cref="SetActivity"/> from your MainActivity's OnCreate.
        /// </summary>
        internal static Activity? CurrentActivity { get; private set; }

        public IReadOnlyDictionary<uint, INativeNotification> ActiveNotifications => _notifications;

        public AndroidNotificationChannelManager ChannelManager { get; }
        NotificationChannelManager INativeNotificationManagerImpl.ChannelManager => ChannelManager;
        public bool ClearOnClose { get; set; }

        public event EventHandler<NativeNotificationCompletedEventArgs>? NotificationCompleted;

        public NativeNotificationManager(Context context)
        {
            _context = context;

            ChannelManager = new AndroidNotificationChannelManager(context);
        }

        /// <summary>
        /// Registers the current Activity for permission requests and intent handling.
        /// Call this from your MainActivity's OnCreate or OnResume.
        /// </summary>
        public static void SetActivity(Activity activity)
        {
            CurrentActivity = activity;

            if (Notifications.NativeNotificationManager.Current is NativeNotificationManager manager
                && activity is IActivityIntentResultHandler handler)
            {
                handler.OnActivityIntent += manager.Activity_OnActivityIntent;
            }
        }

        private void Activity_OnActivityIntent(object? sender, Intent e)
        {
            if (e.Extras?.GetString("type") == "notification")
            {
                (Notifications.NativeNotificationManager.Current as NativeNotificationManager)?.OnReceivedIntent(e);
            }
        }

        public void CloseAll()
        {
            foreach (var notification in _notifications)
            {
                notification.Value?.Close();
            }

            NotificationManagerCompat.From(_context).CancelAll();

            _notifications.Clear();
        }

        public INativeNotification? CreateNotification(string? category)
        {
            if (!_isActive || _context == null)
                return null;

            var channel = ChannelManager?.GetChannel(category ?? AndroidNotificationChannelManager.DefaultChannel) ??
                ChannelManager?.AddChannel(new NotificationChannel(AndroidNotificationChannelManager.DefaultChannel, AndroidNotificationChannelManager.DefaultChannelLabel));

            if (channel == null)
            {
                return null;
            }

            return new NativeNotification(_context, this, channel);
        }

        public async void Initialize(AppNotificationOptions? options)
        {
            ChannelManager.ConsolidateChannels();
            _isActive = await CheckPermission();
        }

        private async Task<bool> CheckPermission()
        {
            if (_context == null)
                return false;

            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                return true;

            return await PlatformSupport.CheckPermission(_context, Manifest.Permission.PostNotifications);
        }

        internal async void Show(NativeNotification nativeNotification)
        {
            if (_context != null && nativeNotification.CurrentNotification != null && await CheckPermission())
            {
                NotificationManagerCompat.From(_context).Notify((int)nativeNotification.Id, nativeNotification.CurrentNotification);

                _notifications[nativeNotification.Id] = nativeNotification;
            }
        }

        internal void OnReceivedIntent(Intent? intent)
        {
            if (intent == null)
                return;

            var id = intent.Extras?.GetInt("notification-id");
            if (id != null)
            {
                var action = intent.Extras?.GetString("notification-action");
                var userAction = intent.Extras?.GetString("user-action");
                var eventArgs = new NativeNotificationCompletedEventArgs()
                {
                    NotificationId = (uint?)id,
                    IsActivated = action == "activate",
                    IsCancelled = action == "cancel",
                    ActionTag = userAction,
                    UserData = AndroidX.Core.App.RemoteInput.GetResultsFromIntent(intent)?.GetCharSequence(userAction)
                };

                NotificationCompleted?.Invoke(this, eventArgs);
            }
        }

        internal void Close(NativeNotification nativeNotification)
        {
            NotificationManagerCompat.From(_context).Cancel((int)nativeNotification.Id);
            _notifications.Remove(nativeNotification.Id);
        }

        public void Dispose()
        {
            if (ClearOnClose)
                CloseAll();
        }
    }
}
#endif
