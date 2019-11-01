using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.AR.Sceneform.Rendering;
using Java.Util.Concurrent;
using UK.CO.Appoly.Arcorelocation;
using UK.CO.Appoly.Arcorelocation.Rendering;
using Google.AR.Core;
using Google.AR.Sceneform;
using Java.Util.Functions;
using UK.CO.Appoly.Arcorelocation.Utils;
using Google.AR.Core.Exceptions;

namespace Xamarin_ArCoreLocation
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IBiFunction, Scene.IOnUpdateListener, ILocationNodeRender
    {
        bool installRequested;
        bool hasFinishedLoading = false;
        Snackbar loadingMessageSnackbar = null;
        ArSceneView arSceneView;
        // Renderables for this example
        ViewRenderable exampleLayoutRenderable;
        CompletableFuture exampleLayout;
        // Our ARCore-Location scene
        LocationScene locationScene = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            arSceneView = (ArSceneView)FindViewById(Resource.Id.ar_scene_view);

            exampleLayout = ViewRenderable.InvokeBuilder().SetView(this, Resource.Layout.example_layout).Build();
            CompletableFuture.AllOf(exampleLayout).Handle(this);
            arSceneView.Scene.AddOnUpdateListener(this);
            ARLocationPermissionHelper.RequestPermission(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (!ARLocationPermissionHelper.HasPermission(this))
            {
                if (!ARLocationPermissionHelper.ShouldShowRequestPermissionRationale(this))
                {
                    // Permission denied with checking "Do not ask again".
                    ARLocationPermissionHelper.LaunchPermissionSettings(this);
                }
                else
                {
                    Toast.MakeText(this, "Camera permission is needed to run this application", ToastLength.Long).Show();
                }

                Finish();
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (locationScene != null)
            {
                locationScene.Resume();
            }

            if (arSceneView.Session == null)
            {
                // If the session wasn't created yet, don't resume rendering.
                // This can happen if ARCore needs to be updated or permissions are not granted yet.
                try
                {
                    Session session = CreateArSession(this, installRequested);
                    if (session == null)
                    {
                        installRequested = ARLocationPermissionHelper.HasPermission(this);
                        return;
                    }
                    else
                    {
                        arSceneView.SetupSession(session);
                    }
                }
                catch (UnavailableException e)
                {
                    string message;
                    if (e is UnavailableArcoreNotInstalledException) 
                    {
                        message = "Please install ARCore";
                    } 
                    else if (e is UnavailableApkTooOldException) 
                    {
                        message = "Please update ARCore";
                    } 
                    else if (e is UnavailableSdkTooOldException) 
                    {
                        message = "Please update this app";
                    } 
                    else if (e is UnavailableDeviceNotCompatibleException) 
                    {
                        message = "This device does not support AR";
                    } 
                    else
                    {
                        message = "Failed to create AR session";
                    }
                    Toast.MakeText(this, message, ToastLength.Long).Show();
                }
            }

            try
            {
                arSceneView.Resume();
            }
            catch (System.Exception ex)
            {
                Finish();
                return;
            }

            if (arSceneView.Session != null)
            {
                ShowLoadingMessage();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (locationScene != null)
            {
                locationScene.Pause();
            }

            arSceneView.Pause();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            arSceneView.Destroy();
        }

        public static Session CreateArSession(Activity activity, bool installRequested)
        {
            Session session = null;
            // if we have the camera permission, create the session
            if (ARLocationPermissionHelper.HasPermission(activity)) 
            {
                //switch (ArCoreApk.Instance.RequestInstall(activity, !installRequested)) 
                //{
                //    case "INSTALL_REQUESTED":
                //        return null;
                //    case "INSTALLED":
                //        break;
                //}
                session = new Session(activity);
                // IMPORTANT!!!  ArSceneView needs to use the non-blocking update mode.
                Config config = new Config(session);
                config.SetUpdateMode(Config.UpdateMode.LatestCameraImage);
                session.Configure(config);
            }

            return session;
        }

        private Node getExampleView()
        {
            Node baseNode = new Node();
            baseNode.Renderable = exampleLayoutRenderable;
            // Add  listeners etc here
            return baseNode;
        }

        private void ShowLoadingMessage()
        {
            if (loadingMessageSnackbar != null && loadingMessageSnackbar.IsShownOrQueued)
            {
                return;
            }

            loadingMessageSnackbar = Snackbar.Make(this.FindViewById(Android.Resource.Id.Content), Resource.String.plane_finding, Snackbar.LengthIndefinite);
            loadingMessageSnackbar.View.SetBackgroundColor(Android.Graphics.Color.Blue);
            loadingMessageSnackbar.Show();
        }

        private void HideLoadingMessage()
        {
            if (loadingMessageSnackbar == null)
            {
                return;
            }

            loadingMessageSnackbar.Dismiss();
            loadingMessageSnackbar = null;
        }

        public Java.Lang.Object Apply(Java.Lang.Object t, Java.Lang.Object u)
        {
            if (u != null)
            {
                return null;
            }

            try
            {
                exampleLayoutRenderable = (ViewRenderable)exampleLayout.Get();
                hasFinishedLoading = true;
            }
            catch (System.Exception) { }

            return null;
        }

        public void OnUpdate(FrameTime p0)
        {
            if (!hasFinishedLoading)
            {
                return;
            }

            if (locationScene == null)
            {
                // If our locationScene object hasn't been setup yet, this is a good time to do it
                // We know that here, the AR components have been initiated.
                locationScene = new LocationScene(this, this, arSceneView);

                // Now lets create our location markers.
                // First, a layout
                LocationMarker layoutLocationMarker = new LocationMarker(-0.119677, 51.478494, getExampleView());

                // An example "onRender" event, called every frame
                // Updates the layout with the markers distance
                layoutLocationMarker.RenderEvent = this;
                // Adding the marker
                locationScene.MLocationMarkers.Add(layoutLocationMarker);
            }

            Frame frame = arSceneView.ArFrame;
            if (frame == null)
            {
                return;
            }

            if (frame.Camera.TrackingState != TrackingState.Tracking)
            {
                return;
            }

            if (locationScene != null)
            {
                locationScene.ProcessFrame(frame);
            }

            if (loadingMessageSnackbar != null)
            {
                foreach (Plane plane in frame.GetUpdatedTrackables(Java.Lang.Class.FromType(typeof(Plane))))
                {
                    if (plane.TrackingState == TrackingState.Tracking)
                    {
                        HideLoadingMessage();
                    }
                }
            }

            return;
        }

        public void Render(LocationNode node)
        {
            View eView = exampleLayoutRenderable.View;
            //TextView distanceTextView = (TextView)eView.FindViewById(Resource.Id.textView2);
            //distanceTextView.SetText(node.Distance.ToString(), TextView.BufferType.Normal);
        }
    }
}

