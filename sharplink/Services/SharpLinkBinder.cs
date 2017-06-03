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
    class SharpLinkBinder : Binder
    {
        private SharpLinkService mService;
        private MainActivity client;

        public SharpLinkBinder(SharpLinkService mService)
        {
            this.mService = mService;
        }

        public void setClient(MainActivity client) {
            this.client = client;
        }

        public void Log(string log) {
            client?.Log(log);
        }

        public bool IsConnected()
        {
            return mService.IsConnected();
        }

        public Dictionary<string, string> GetSharpLinkConfig() {
            Dictionary<string, string> res = new Dictionary<string, string>() {
                { "toxid", mService.toxid},
                { "port", mService.port},
                {"local_port", mService.local_port }
            };
            return res;
        }
    }
}