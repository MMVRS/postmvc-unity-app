using System;
using Build1.PostMVC.Core.MVCS.Mediation;
using Build1.PostMVC.Unity.App.Mediation;
using Build1.PostMVC.Unity.App.Modules.Assets;
using Build1.PostMVC.Unity.App.Modules.Device;

namespace Build1.PostMVC.Unity.App.Modules.UI.Screens
{
    public sealed class ScreenConfig : UIControlConfiguration
    {
        public readonly ScreenSurfaceKind surfaceKind;

        public ScreenConfig(string asset, int layerId) : this(asset, layerId, ScreenSurfaceKind.UGUI) { }
        public ScreenConfig(string asset, int layerId, Enum assetBundleId) : this(asset, layerId, assetBundleId, ScreenSurfaceKind.UGUI) { }
        public ScreenConfig(string asset, int layerId, AssetBundleInfo assetBundleInfo) : this(asset, layerId, assetBundleInfo, ScreenSurfaceKind.UGUI) { }

        public ScreenConfig(string asset, int layerId, ScreenSurfaceKind surfaceKind) : base(asset, layerId)
        {
            this.surfaceKind = surfaceKind;
        }

        public ScreenConfig(string asset, int layerId, Enum assetBundleId, ScreenSurfaceKind surfaceKind) : base(asset, layerId, assetBundleId)
        {
            this.surfaceKind = surfaceKind;
        }

        public ScreenConfig(string asset, int layerId, AssetBundleInfo assetBundleInfo, ScreenSurfaceKind surfaceKind) : base(asset, layerId, assetBundleInfo)
        {
            this.surfaceKind = surfaceKind;
        }

        /*
         * Platform.
         */

        public ScreenConfig SetPlatform(DevicePlatform platform)
        {
            DevicePlatform = platform;
            return this;
        }

        /*
         * Device.
         */

        public ScreenConfig SetDeviceType(DeviceType type)
        {
            DeviceType = type;
            return this;
        }

        /*
         * Bindings.
         */

        public new ScreenConfig AddBinding<V, M>() where V : IUnityView
                                                   where M : Mediator
        {
            base.AddBinding<V, M>();
            return this;
        }

        public new ScreenConfig AddBinding<V, I, M>() where V : IUnityView, I
                                                      where M : Mediator
        {
            base.AddBinding<V, I, M>();
            return this;
        }
    }
}
