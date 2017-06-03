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

namespace sharplink.Services
{
    class SharpLinkConnection : Java.Lang.Object, IServiceConnection
    {
        public bool IsConnected { get; private set; }
        public SharpLinkBinder Binder { get; private set; }
        private MainActivity client;

        public SharpLinkConnection(MainActivity client) {
            IsConnected = false;
            Binder = null;
            this.client = client;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as SharpLinkBinder;
            IsConnected = Binder != null;
            Binder.setClient(client);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            IsConnected = false;
            Binder.setClient(null);
            Binder = null;
        }
    }
}