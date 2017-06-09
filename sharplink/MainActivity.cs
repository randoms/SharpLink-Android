using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using SkynetAndroid.Utils;
using Android.Preferences;
using sharplink.Services;
using Android.Graphics;
using System.Threading;
using System.Collections.Generic;
using Android.Media;
using Android.Support.V4.App;

namespace sharplink
{
    [Activity(Theme = "@style/AppTheme", Label = "SharpLink", Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private SharpLinkAndroid.Connection mSharpLink;
        Button button;
        ScrollView mscroll;
        EditText serverID;
        EditText portText;
        TextView status;
        bool connectFlag;
        ISharedPreferences prefs;
        private SharpLinkConnection mConnection;
        bool runningFlag = true;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it

            button = FindViewById<Button>(Resource.Id.myButton);
            mscroll = FindViewById<ScrollView>(Resource.Id.status_scroll);
            serverID = FindViewById<EditText>(Resource.Id.toxid);
            portText = FindViewById<EditText>(Resource.Id.port);
            status = FindViewById<TextView>(Resource.Id.console);

            prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            string serverIDStr = prefs.GetString("toxid", "");
            string portStr = prefs.GetString("port", "");

            if (mConnection == null)
            {
                mConnection = new SharpLinkConnection(this);
            }


            button.Click += delegate
            {
                if (connectFlag)
                {
                    button.Text = "CONNECT";
                    serverID.Enabled = true;
                    portText.Enabled = true;
                    connectFlag = false;
                    return;
                }
                startServer(serverID.Text, portText.Text);
            };

            if ("" != serverIDStr && "" != portStr)
            {
                serverID.Text = serverIDStr;
                portText.Text = portStr;
                startServer(serverIDStr, portStr);
            }

            // 开启定时确认连接状态
            Task.Run(() =>
            {
                while (runningFlag)
                {
                    Thread.Sleep(1000);
                    if (!mConnection.IsConnected)
                    {
                        setOnlineStatus(false);
                        continue;
                    }
                    if (mConnection.Binder.IsConnected())
                    {
                        setOnlineStatus(true);
                    }
                    else
                    {
                        setOnlineStatus(false);
                    }
                }
            });
        }

        private void notify()
        {
            //AudioManager manager = (AudioManager)GetSystemService(AudioService);
            //int volume = manager.GetStreamVolume(Stream.Notification);
            //manager.SetStreamVolume(Stream.Notification, 100, 0);


            //var notification = RingtoneManager
            //        .GetDefaultUri(RingtoneType.Notification);

            //MediaPlayer player = MediaPlayer.Create(this, notification);
            //player.Looping = false;
            //player.Start();
            //while (player.IsPlaying) {
            //    Thread.Sleep(500);
            //}
            //manager.SetStreamVolume(Stream.Notification, volume, 0);
            //Define Notification Manager
            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);

            //Define sound URI
            var soundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            NotificationCompat.Builder mBuilder = new NotificationCompat.Builder(this)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentTitle("SharpLink")
                    .SetContentText("SharpLink连接状态改变")
                    .SetSound(soundUri, (int)Stream.Notification); //This sets the sound to play

            //Display notification
            notificationManager.Notify(0, mBuilder.Build());
        }

        private void startServer(string toxid, string port)
        {
            Intent serviceToStart = new Intent(this, typeof(SharpLinkService));
            serviceToStart.PutExtra("toxid", toxid);
            serviceToStart.PutExtra("port", port);
            var currentConf = getSharpLinkConf();
            if (currentConf != null && (toxid != currentConf["toxid"] || port != currentConf["port"]))
            {
                Log("Stop Service");
                StopService(serviceToStart);
                UnbindService(mConnection);
            }
            Log("Start Server");
            StartService(serviceToStart);
            BindService(serviceToStart, mConnection, Bind.AutoCreate);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutString("toxid", toxid);
            editor.PutString("port", port);
            editor.Apply();

            connectFlag = true;
            button.Text = "CONNECTED";
            serverID.Enabled = false;
            portText.Enabled = false;
        }

        bool previousOnlineStatus = false;
        private void setOnlineStatus(bool status)
        {
            RunOnUiThread(() =>
            {
                if (previousOnlineStatus != status)
                {
                    notify();
                    previousOnlineStatus = status;
                }

                if (status)
                {
                    button.SetTextColor(Color.Rgb(0x8B, 0xC3, 0x4A));
                }
                else
                {
                    button.SetTextColor(Color.Black);
                }
            });

        }

        public void Log(string log)
        {
            RunOnUiThread(() =>
            {
                int startIndex = status.Text.Length - 2000;
                if (startIndex < 0) startIndex = 0;
                status.Text = status.Text.Substring(startIndex) + "Time: " + Utils.UnixTimeNow() + ", " + log + "\n";
                mscroll.Post(() =>
                {
                    mscroll.FullScroll(FocusSearchDirection.Down);
                });
            });
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnDestroy()
        {
            runningFlag = false;
            base.OnDestroy();
        }

        public Dictionary<string, string> getSharpLinkConf()
        {
            if (!mConnection.IsConnected)
                return null;
            return mConnection.Binder.GetSharpLinkConfig();
        }

    }


}


