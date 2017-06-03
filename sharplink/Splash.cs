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
using System.Threading.Tasks;
using System.Threading;

namespace sharplink
{
    [Activity(Label = "SharpLink", MainLauncher = true, Icon = "@drawable/icon")]
    public class Splash : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.SetFlags(WindowManagerFlags.Fullscreen,
                    WindowManagerFlags.Fullscreen);
            SetContentView(Resource.Layout.splash);
            Task.Run(() => {
                Thread.Sleep(1000);
                Intent intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
                OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
                Finish();
            });
        }
    }
}