using System;
using Build1.PostMVC.Core.MVCS.Mediation;
using Build1.PostMVC.Unity.App.Modules.Assets;
using Build1.PostMVC.Unity.App.Modules.Device;
using Build1.PostMVC.Unity.App.Modules.UI.Screens;

namespace Build1.PostMVC.Unity.App.Modules.UI.Popups
{
    public sealed class PopupConfig : UIControlConfiguration
    {
        public readonly ScreenSurfaceKind surfaceKind;

        public PopupConfig(string asset, int layerId) : this(asset, layerId, ScreenSurfaceKind.UGUI) { }
        public PopupConfig(string asset, int layerId, Enum assetBundleId) : this(asset, layerId, assetBundleId, ScreenSurfaceKind.UGUI) { }
        public PopupConfig(string asset, int layerId, AssetBundleInfo assetBundleInfo) : this(asset, layerId, assetBundleInfo, ScreenSurfaceKind.UGUI) { }

        public PopupConfig(string asset, int layerId, ScreenSurfaceKind surfaceKind) : base(asset, layerId)
        {
            this.surfaceKind = surfaceKind;
        }

        public PopupConfig(string asset, int layerId, Enum assetBundleId, ScreenSurfaceKind surfaceKind) : base(asset, layerId, assetBundleId)
        {
            this.surfaceKind = surfaceKind;
        }

        public PopupConfig(string asset, int layerId, AssetBundleInfo assetBundleInfo, ScreenSurfaceKind surfaceKind) : base(asset, layerId, assetBundleInfo)
        {
            this.surfaceKind = surfaceKind;
        }

        /*
         * Platform.
         */

        public PopupConfig SetPlatform(DevicePlatform platform)
        {
            DevicePlatform = platform;
            return this;
        }

        /*
         * Device.
         */

        public PopupConfig SetDeviceType(DeviceType type)
        {
            DeviceType = type;
            return this;
        }

        /*
         * Bindings.
         */
        
        public new PopupConfig AddBinding<V, M>() where V : IPopupView
                                                  where M : Mediator
        {
            base.AddBinding<V, M>();
            return this;
        }

        public new PopupConfig AddBinding<V, I, M>() where V : IPopupView, I
                                                     where M : Mediator
        {
            base.AddBinding<V, I, M>();
            return this;
        }
    }
}
