using System;
using System.IO;
using Build1.PostMVC.Unity.App.Modules.Assets.Impl.Cache;
using UnityEngine;

namespace Build1.PostMVC.Unity.App.Modules.Assets.Impl.Agents
{
    internal sealed class AssetsAgentWebGL : AssetsAgentBase
    {
        public override void LoadAsync(AssetBundleInfo info,
                                       Func<AssetBundleInfo, AssetBundleCacheInfo> onCacheInfoGet,
                                       Action<bool, AssetBundleInfo> onCacheStateDetermined,
                                       Action<AssetBundleInfo> onCacheInfoClean,
                                       Action<string, AssetBundleInfo> onCacheInfoRecord,
                                       Action<AssetBundleInfo, float, ulong> onProgress,
                                       Action<AssetBundleInfo, AssetBundle> onComplete,
                                       Action<AssetBundleInfo, AssetsException> onError)
        {
            if (info.IsEmbedBundle)
                info.OverrideBundleUrl(Path.Combine(Application.streamingAssetsPath, info.BundleId));
            else if (!info.IsRemoteBundle)
                throw new AssetsException(AssetsExceptionType.UnknownBundleType);

            StartCoroutine(LoadRemoteAssetBundleCoroutine(info, null, (_, __) => { }, null, null, onProgress, onComplete, onError));
        }
    }
}
