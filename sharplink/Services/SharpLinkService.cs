using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SharpToxAndroid.Core;
using SkynetAndroid.Utils;
using System.Threading.Tasks;

namespace sharplink.Services
{
    //[Service(Name = "org.bwbot.sharplink.service",Process=":sharplink",Exported=true,Permission = "signature")]
    [Service(Name = "org.bwbot.sharplink.service")]
    class SharpLinkService : Service
    {

        private SharpLinkBinder mBinder;
        SharpLinkAndroid.Connection mSharpLink;
        public string toxid;
        public string port;
        public string local_port = "23232";


        public override void OnCreate()
        {
            mSharpLink = new SharpLinkAndroid.Connection();
        }

        public override IBinder OnBind(Intent intent)
        {
            string newToxid = intent.GetStringExtra("toxid");
            string newPort = intent.GetStringExtra("port");
            mBinder = new SharpLinkBinder(this);
            Utils.setLog(mBinder.Log);
            if (toxid != newToxid || port != newPort)
            {
                toxid = newToxid;
                port = newPort;
                Task.Run(() =>
                {
                    mSharpLink.Connect(new string[] { "23232", toxid, "192.168.0.123", port });
                });
            }
            return mBinder;
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            mBinder = null;
            base.OnDestroy();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            string newToxid = intent.GetStringExtra("toxid");
            string newPort = intent.GetStringExtra("port");
            mBinder = new SharpLinkBinder(this);
            Utils.setLog(mBinder.Log);
            if (toxid != newToxid || port != newPort)
            {
                toxid = newToxid;
                port = newPort;
                Task.Run(() =>
                {
                    mSharpLink.Connect(new string[] { "23232", toxid, "192.168.0.123", port });
                });
            }
            return StartCommandResult.RedeliverIntent;
        }

        public bool IsConnected()
        {
            return mSharpLink.IsConnected;
        }
    }
}