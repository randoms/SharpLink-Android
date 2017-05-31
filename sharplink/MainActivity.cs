using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using SkynetAndroid.Utils;

namespace sharplink
{
	[Activity (Label = "sharplink", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		int count = 1;
        private SharpLinkAndroid.Connection mSharpLink;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
            ScrollView mscroll = FindViewById<ScrollView>(Resource.Id.status_scroll);
            EditText serverID = FindViewById<EditText>(Resource.Id.toxid);
            EditText port = FindViewById<EditText>(Resource.Id.port);

            Utils.setLogFile(FindViewById<TextView>(Resource.Id.console), mscroll, this);
            bool connectFlag = false;
            button.Click += delegate {
                if (connectFlag) {
                    Utils.Log("Already Connected");
                    return;
                }
                    
                mSharpLink = new SharpLinkAndroid.Connection();
                
                Utils.Log("Start server");
                Task.Run(() => {
                    mSharpLink.Connect(new string[] { "23232", serverID.Text, "127.0.0.1", port.Text });
                });
                connectFlag = true;
            };
		}



        //public void setStatus(String statusStr)
        //{
        //    status.append(statusStr + "\n");
        //    statusScroll.post(new Runnable() {
        //    @Override
        //    public void run()
        //    {
        //        statusScroll.fullScroll(View.FOCUS_DOWN);
        //    }
        //});
    }


}


