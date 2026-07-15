using Build1.PostMVC.Core.MVCS.Commands;
using Build1.PostMVC.Core.MVCS.Injection;

namespace Build1.PostMVC.Unity.App.Modules.Assets.Commands
{
    [Poolable]
    public sealed class AssetBundleTryLoadCommand : Command<AssetBundleInfo>
    {
        [Inject] public IAssetsController AssetsController { get; set; }

        public override void Execute(AssetBundleInfo info)
        {
            if (AssetsController.CheckBundleLoadedOrLoading(info))
                return;

            Retain();
            AssetsController.LoadBundle(info, _ => Release(), Fail);
        }
    }
}
