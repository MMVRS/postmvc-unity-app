using System;

namespace Build1.PostMVC.Unity.App.Modules.Assets
{
    public interface IAssetsController
    {
        event Func<string, AssetBundleInfo> OnBundleInfoRequest;
        event Func<Enum, string>            OnBundleStringIdentifier;

        /*
         * Check.
         */

        bool CheckBundleLoading(Enum identifier);
        bool CheckBundleLoading(string identifier);
        bool CheckBundleLoading(AssetBundleInfo info);
        
        bool CheckBundleLoaded(Enum identifier);
        bool CheckBundleLoaded(string identifier);
        bool CheckBundleLoaded(AssetBundleInfo info);
        
        bool CheckBundleLoadedOrLoading(Enum identifier);
        bool CheckBundleLoadedOrLoading(string identifier);
        bool CheckBundleLoadedOrLoading(AssetBundleInfo info);

        void CollectLoadedBundleIds(System.Collections.Generic.List<string> output);

        /*
         * Embed.
         */

        void LoadEmbedBundle(Enum identifier);
        void LoadEmbedBundle(string identifier);

        void LoadEmbedBundle(Enum identifier, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);
        void LoadEmbedBundle(string identifier, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);

        /*
         * Remote.
         */

        void LoadRemoteBundle(string url);
        void LoadRemoteBundle(string url, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);

        void LoadRemoteOrCachedBundle(string url, uint version);
        void LoadRemoteOrCachedBundle(string url, uint version, string cacheId);
        void LoadRemoteOrCachedBundle(string url, uint version, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);
        void LoadRemoteOrCachedBundle(string url, uint version, string cacheId, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);

        /*
         * Loading by Info.
         */

        void LoadBundle(AssetBundleInfo info);
        void LoadBundle(AssetBundleInfo info, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError);

        /*
         * Loading Aborting.
         */

        void AbortBundleLoading(Enum identifier);
        void AbortBundleLoading(string identifier);
        void AbortBundleLoading(AssetBundleInfo info);

        /*
         * Unloading.
         */

        void UnloadBundle(Enum identifier, bool unloadObjects);
        void UnloadBundle(string identifier, bool unloadObjects);
        void UnloadBundle(AssetBundleInfo bundleInfo, bool unloadObjects);

        void UnloadAllBundles(bool unloadObjects);
        void UnloadAllBundles(bool unloadObjects, params Enum[] except);

        /*
         * Getting.
         */

        AssetBundleInfo GetBundle(Enum identifier);
        AssetBundleInfo GetBundle(string identifier);

        bool TryGetBundle(Enum identifier, out AssetBundleInfo info);
        bool TryGetBundle(string identifier, out AssetBundleInfo info);

        /*
         * Cache.
         */

        bool CheckBundleCached(AssetBundleInfo info);
        ulong GetBundleCacheSizeByCacheId(string cacheId);
        ulong GetCachedFilesSizeBytes();
        void  CleanCache();

        /*
         * Assets.
         */

        T GetAsset<T>(Enum identifier, string assetName) where T : UnityEngine.Object;
        T GetAsset<T>(string identifier, string assetName) where T : UnityEngine.Object;
        T GetAsset<T>(AssetBundleInfo bundleInfo, string assetName) where T : UnityEngine.Object;

        UnityEngine.AssetBundleRequest LoadAssetAsync<T>(AssetBundleInfo bundleInfo, string assetName) where T : UnityEngine.Object;

        bool TryGetAsset<T>(Enum identifier, string assetName, out T asset) where T : UnityEngine.Object;
        bool TryGetAsset<T>(string identifier, string assetName, out T asset) where T : UnityEngine.Object;
        bool TryGetAsset<T>(AssetBundleInfo bundleInfo, string assetName, out T asset) where T : UnityEngine.Object;
    }
}
