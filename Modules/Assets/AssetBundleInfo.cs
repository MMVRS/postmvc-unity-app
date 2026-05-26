using System;
using System.Collections.Generic;

namespace Build1.PostMVC.Unity.App.Modules.Assets
{
    public sealed class AssetBundleInfo
    {
        public string BundleId        { get; private set; }
        public string BundleUrl       { get; private set; }
        public uint   BundleVersion   { get; private set; }
        public string CacheId         { get; private set; }
        public float  LoadingProgress { get; private set; }
        public ulong  DownloadedBytes { get; private set; }

        internal List<string>            AtlasesNames { get; private set; }
        internal UnityEngine.AssetBundle Bundle       { get; private set; }

        public bool IsEmbedBundle      { get; private set; }
        public bool IsRemoteBundle     => !IsEmbedBundle;
        public bool IsLoading          { get; private set; }
        public bool IsLoaded           => Bundle != null;
        public bool IsAborted          { get; private set; }
        public bool IsCacheReset       { get; private set; }
        public bool IsLoadingFromCache { get; private set; }

#if UNITY_EDITOR
        public string EditorDebugReason { get; private set; }
#endif

        internal bool HasAtlases     => AtlasesNames != null && AtlasesNames.Count > 0;
        internal bool IsCacheEnabled { get; private set; }

        private AssetBundleInfo()
        {
        }

        /*
         * Public.
         */

        public AssetBundleInfo AddAtlasesNames(params string[] atlasesNames)
        {
            if (AtlasesNames == null)
                AtlasesNames = new List<string>(atlasesNames);
            else
                AtlasesNames.AddRange(atlasesNames);

            return this;
        }

#if UNITY_EDITOR
        public AssetBundleInfo SetEditorDebugReason(string reason)
        {
            EditorDebugReason = reason;
            return this;
        }
#endif

        internal void OverrideBundleUrl(string url)
        {
            BundleUrl = url;
        }

        internal void SetLoading()
        {
            IsLoading = true;
            IsAborted = false;
        }

        internal void SetAborted()
        {
            IsAborted = true;
        }

        internal void SetLoadingProgress(float progress, ulong downloadedBytes)
        {
            LoadingProgress = progress;
            DownloadedBytes = downloadedBytes;
        }

        internal void SetLoaded(UnityEngine.AssetBundle bundle)
        {
            IsLoading = false;
            Bundle = bundle;
        }

        internal void Clean()
        {
            IsLoading = false;
            IsAborted = false;
            Bundle = null;
#if UNITY_EDITOR
            EditorDebugReason = null;
#endif
        }

        internal void SetCacheReset()
        {
            IsCacheReset = true;
        }

        internal void SetCacheRecorded()
        {
            IsCacheReset = false;
        }

        internal void SetLoadingFromCache(bool value)
        {
            IsLoadingFromCache = value;
        }

        internal void Update(AssetBundleInfo info)
        {
            if (BundleId != info.BundleId)
                throw new AssetsException(AssetsExceptionType.BundleInfoUpdateError);

            BundleUrl = info.BundleUrl;
            BundleVersion = info.BundleVersion;
            CacheId = info.CacheId;
            LoadingProgress = info.LoadingProgress;
            DownloadedBytes = info.DownloadedBytes;

            AtlasesNames = info.AtlasesNames;
            Bundle = info.Bundle;

            IsEmbedBundle = info.IsEmbedBundle;
            IsLoading = info.IsLoading;
            IsAborted = info.IsAborted;

            IsCacheEnabled = info.IsCacheEnabled;
            IsCacheReset = info.IsCacheReset;
            IsLoadingFromCache = info.IsLoadingFromCache;
#if UNITY_EDITOR
            EditorDebugReason = info.EditorDebugReason;
#endif
        }

        public string[] GetAllScenePaths() { return Bundle.GetAllScenePaths(); }
        public string[] GetAllAssetNames() { return Bundle.GetAllAssetNames(); }

        public override string ToString()
        {
            return BundleId;
        }

        /*
         * Static.
         */

        public static AssetBundleInfo FromId(Enum bundleId)
        {
            return new AssetBundleInfo { BundleId = EnumToStringIdentifier(bundleId), IsEmbedBundle = true };
        }

        public static AssetBundleInfo FromId(string bundleId)
        {
            return new AssetBundleInfo { BundleId = bundleId, IsEmbedBundle = true };
        }

        public static AssetBundleInfo FromUrl(string bundleUrl)
        {
            return new AssetBundleInfo
            {
                BundleId = bundleUrl,
                BundleUrl = bundleUrl,
                IsCacheEnabled = false,
                IsEmbedBundle = false
            };
        }

        public static AssetBundleInfo FromUrlCached(string bundleUrl, uint version)
        {
            return new AssetBundleInfo
            {
                BundleId = bundleUrl,
                BundleUrl = bundleUrl,
                BundleVersion = version,
                IsCacheEnabled = true,
                IsEmbedBundle = false
            };
        }

        public static AssetBundleInfo FromUrlCached(string bundleUrl, uint version, string cacheId)
        {
            return new AssetBundleInfo
            {
                BundleId = bundleUrl,
                BundleUrl = bundleUrl,
                BundleVersion = version,
                CacheId = cacheId,
                IsCacheEnabled = true,
                IsEmbedBundle = false
            };
        }

        public static string EnumToStringIdentifier(Enum identifier)
        {
            return identifier.ToString().ToLower();
        }
    }
}
