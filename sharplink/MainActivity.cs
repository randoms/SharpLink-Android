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

namespace sharplink
{
	[Activity (Theme = "@style/AppTheme", Label = "SharpLink", Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
        private SharpLinkAndroid.Connection mSharpLink;
        Button button;
        ScrollView mscroll;
        EditText serverID;
        EditText port;
        bool connectFlag;

        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it

            button = FindViewById<Button>(Resource.Id.myButton);
            mscroll = FindViewById<ScrollView>(Resource.Id.status_scroll);
            serverID = FindViewById<EditText>(Resource.Id.toxid);
            port = FindViewById<EditText>(Resource.Id.port);

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            string serverIDStr = prefs.GetString("toxid", "");
            string portStr = prefs.GetString("port", "");

            Utils.setLogFile(FindViewById<TextView>(Resource.Id.console), mscroll, this);
            
            mSharpLink = new SharpLinkAndroid.Connection();

            button.Click += delegate
            {
                if (connectFlag)
                {
                    stopServer();
                    return;
                }
                startServer();
            };

            if ("" != serverIDStr && "" != portStr)
            {
                serverID.Text = serverIDStr;
                port.Text = portStr;
                startServer();
            }
        }

        private void stopServer() {
            Utils.Log("Stop Server", true);
            button.Text = "CONNECT";
            serverID.Enabled = true;
            port.Enabled = true;
            mSharpLink.Stop();
            connectFlag = false;
            return;
        }

        private void startServer() {
            Utils.Log("Start Server", true);
            Task.Run(() =>
            {
                ISharedPreferences prefs1 = PreferenceManager.GetDefaultSharedPreferences(this);
                ISharedPreferencesEditor editor1 = prefs1.Edit();
                editor1.PutString("toxid", serverID.Text);
                editor1.PutString("port", port.Text);
                editor1.Apply();
                mSharpLink.Connect(new string[] { "23232", serverID.Text, "127.0.0.1", port.Text });
            });
            connectFlag = true;
            button.Text = "CONNECTED";
            serverID.Enabled = false;
            port.Enabled = false;
        }

    }


}


