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
using Android.Content;
using Google.AR.Core.Exceptions;
using Java.Lang;

namespace Xamarin_ArCoreLocation
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        ArSceneView arSceneView;

        // Renderables for this example
        ViewRenderable exampleLayoutRenderable;

        // Our ARCore-Location scene
        LocationScene locationScene = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            arSceneView = (ArSceneView)FindViewById(Resource.Id.ar_scene_view);

            CompletableFuture exampleLayout = ViewRenderable.InvokeBuilder().SetView(this, Resource.Layout.example_layout).Build();
            IBiFunction renderable = new RenderableGetter(ref exampleLayoutRenderable, ref exampleLayout);
            exampleLayout.Handle(renderable);
            arSceneView.Scene.AddOnUpdateListener(new OnUpdateListener(locationScene, arSceneView, this, this, exampleLayoutRenderable));
            ARLocationPermissionHelper.RequestPermission(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (locationScene != null)
            {
                locationScene.Resume();
            }

            try
            {
                arSceneView.Resume();
            }
            catch (CameraNotAvailableException ex)
            {
                Finish();
                return;
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
	}

    class LocationNodeRender : ILocationNodeRender
    {
        ViewRenderable exampleLayoutRenderable;

        public LocationNodeRender(ref ViewRenderable layoutRenderable)
        {
            exampleLayoutRenderable = layoutRenderable;
        }

        public IntPtr Handle => Thread.CurrentThread().Handle;

        public void Dispose()
        {
        }

        public void Render(LocationNode node)
        {
            View eView = exampleLayoutRenderable.View;
            TextView distanceTextView = (TextView)eView.FindViewById(Resource.Id.textView2);
            distanceTextView.SetText(node.Distance);
        }
    }

    class OnUpdateListener : Google.AR.Sceneform.Scene.IOnUpdateListener
    {
        LocationScene locationScene;
        ArSceneView arSceneView;
        Context context;
        Activity activity;
        ViewRenderable exampleLayoutRenderable;

        public OnUpdateListener(LocationScene loc, ArSceneView arsc, Context cont, Activity act, ViewRenderable vr)
        {
            locationScene = loc;
            arSceneView = arsc;
            context = cont;
            activity = act;
            exampleLayoutRenderable = vr;
        }

        public IntPtr Handle => Thread.CurrentThread().Handle;

        public void Dispose()
        {
        }

        public void OnUpdate(FrameTime p0)
        {
            if (locationScene == null)
            {
                // If our locationScene object hasn't been setup yet, this is a good time to do it
                // We know that here, the AR components have been initiated.
                locationScene = new LocationScene(context, activity, arSceneView);

                // Now lets create our location markers.
                // First, a layout
                LocationMarker layoutLocationMarker = new LocationMarker(-65.410517, -24.787947, getExampleView());

                // An example "onRender" event, called every frame
                // Updates the layout with the markers distance
                layoutLocationMarker.RenderEvent = new LocationNodeRender(ref exampleLayoutRenderable);

                // Adding the marker
                locationScene.MLocationMarkers.Add(layoutLocationMarker);

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
            }
            return;
        }

        private Node getExampleView()
        {
            Node baseNode = new Node();
            baseNode.Renderable = exampleLayoutRenderable;
            // Add  listeners etc here
            View eView = exampleLayoutRenderable.View;
            return baseNode;
        }
    }

    class RenderableGetter : IBiFunction
    {
        ViewRenderable exampleLayoutRenderable;
        CompletableFuture exampleLayout;

        public RenderableGetter(ref ViewRenderable vr, ref CompletableFuture cf)
        {
            exampleLayoutRenderable = vr;
            exampleLayout = cf;
        }

        public IntPtr Handle => throw new NotImplementedException();

        public Java.Lang.Object Apply(Java.Lang.Object t, Java.Lang.Object u)
        {
            if (u != null)
            {
                return null;
            }

            try
            {
                exampleLayoutRenderable = (ViewRenderable)exampleLayout.Get();
            }
            catch (System.Exception) {}

            return null;
        }

        public void Dispose()
        {
        }
    }

    class BiFunctionTest : IBiFunction
    {
        public IntPtr Handle => throw new NotImplementedException();

        public Java.Lang.Object Apply(Java.Lang.Object t, Java.Lang.Object u)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}

