using System;
using System.Collections.Generic;
using Build1.PostMVC.Core.MVCS.Events;
using Build1.PostMVC.Core.MVCS.Injection;
using Build1.PostMVC.Unity.App.Modules.Agents;
using Build1.PostMVC.Unity.App.Modules.Assets.Impl.Agents;
using Build1.PostMVC.Unity.App.Modules.Assets.Impl.Cache;
using Build1.PostMVC.Unity.App.Modules.Logging;
using UnityEngine;
using UnityEngine.U2D;
#if UNITY_EDITOR
using System.Text;
#endif

namespace Build1.PostMVC.Unity.App.Modules.Assets.Impl
{
    internal sealed class AssetsController : IAssetsController
    {
        [Log(LogLevel.Warning)] public ILog              Log              { get; set; }
        [Inject]                public IEventDispatcher  Dispatcher       { get; set; }
        [Inject]                public IAgentsController AgentsController { get; set; }
        [Inject]                public IInjectionBinder  InjectionBinder  { get; set; }

        public AssetsAtlasProcessingMode AtlasProcessingMode = AssetsAtlasProcessingMode.Strict;

        public AssetBundleInfoSetting BundleInfoSettings = AssetBundleInfoSetting.RequestMissingInfo |
                                                           AssetBundleInfoSetting.CacheInfo |
                                                           AssetBundleInfoSetting.CreateMissingInfo;

        public event Func<string, AssetBundleInfo> OnBundleInfoRequest;
        public event Func<Enum, string>            OnBundleStringIdentifier;

        private bool BundleInfoRequestingEnabled => (BundleInfoSettings & AssetBundleInfoSetting.RequestMissingInfo) == AssetBundleInfoSetting.RequestMissingInfo;
        private bool BundleInfoCachingEnabled    => (BundleInfoSettings & AssetBundleInfoSetting.CacheInfo) == AssetBundleInfoSetting.CacheInfo;

        private readonly Dictionary<Enum, string>            _ids              = new();
        private readonly Dictionary<string, AssetBundleInfo> _bundles          = new();
        private readonly Dictionary<string, AssetBundleInfo> _bundlesInfoCache = new();
        private readonly List<AssetBundleInfo>               _bundlesLoaded    = new();
        private readonly Dictionary<string, AssetBundleInfo> _bundleByAtlasId  = new();

        private AssetsAgentBase             _agent;
        private AssetBundlesCacheController _cacheController;
        private bool                        _destroying;
        private bool                        _destroyed;

#if UNITY_EDITOR
        private readonly List<string> _editorLoadedBundleIdsBuffer = new(8);
#endif

        [PostConstruct]
        public void PostConstruct()
        {
            InitializeAgent();
            InitializeCache();
        }

        [PreDestroy]
        public void PreDestroy()
        {
            _destroying = true;

            AbortLoadingBundles();

            DisposeAgent();
            DisposeCache();

            _destroyed = true;
            _destroying = false;
        }

        /*
         * Agent.
         */

        private void InitializeAgent()
        {
            if (_agent != null)
                throw new AssetsException(AssetsExceptionType.AgentAlreadyInitialised);

            #if UNITY_WEBGL && !UNITY_EDITOR
            _agent = AgentsController.Create<AssetsAgentWebGL>();
            #else
            _agent = AgentsController.Create<AssetsAgentDefault>();
            #endif

            _agent.AtlasRequested += OnAtlasRequested;
        }

        private void DisposeAgent()
        {
            if (_agent == null)
                return;

            _agent.AtlasRequested -= OnAtlasRequested;
            AgentsController.Destroy(ref _agent);
        }

        /*
         * Check.
         */

        public bool CheckBundleLoading(Enum identifier)
        {
            return CheckBundleLoading(GetBundleStringId(identifier));
        }

        public bool CheckBundleLoading(string identifier)
        {
            return _bundles.TryGetValue(identifier, out var info) && info.IsLoading;
        }

        public bool CheckBundleLoading(AssetBundleInfo info)
        {
            return _bundles.TryGetValue(info.BundleId, out var infoInner) && infoInner.IsLoading;
        }

        public bool CheckBundleLoaded(Enum identifier)
        {
            return CheckBundleLoaded(GetBundleStringId(identifier));
        }

        public bool CheckBundleLoaded(string identifier)
        {
            return _bundles.TryGetValue(identifier, out var info) && info.IsLoaded;
        }

        public bool CheckBundleLoaded(AssetBundleInfo info)
        {
            return _bundles.TryGetValue(info.BundleId, out var infoInner) && infoInner.IsLoaded;
        }

        public bool CheckBundleLoadedOrLoading(Enum identifier)
        {
            return CheckBundleLoadedOrLoading(GetBundleStringId(identifier));
        }

        public bool CheckBundleLoadedOrLoading(string identifier)
        {
            return _bundles.TryGetValue(identifier, out var info) && (info.IsLoaded || info.IsLoading);
        }

        public bool CheckBundleLoadedOrLoading(AssetBundleInfo info)
        {
            return _bundles.TryGetValue(info.BundleId, out var infoInner) && (infoInner.IsLoaded || infoInner.IsLoading);
        }

        public void CollectLoadedBundleIds(List<string> output)
        {
            output.Clear();

            for (var i = 0; i < _bundlesLoaded.Count; i++)
                output.Add(_bundlesLoaded[i].BundleId);
        }

        /*
         * Embed.
         */

        public void LoadEmbedBundle(Enum identifier)   { LoadEmbedBundle(GetBundleStringId(identifier), null, null); }
        public void LoadEmbedBundle(string identifier) { LoadBundle(GetBundleInfoById(identifier), null, null); }

        public void LoadEmbedBundle(Enum identifier, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            LoadEmbedBundle(GetBundleStringId(identifier), onComplete, onError);
        }

        public void LoadEmbedBundle(string identifier, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            LoadBundle(GetBundleInfoById(identifier), onComplete, onError);
        }

        /*
         * Remote.
         */

        public void LoadRemoteBundle(string url)
        {
            LoadBundle(GetBundleInfoByUrl(url), null, null);
        }

        public void LoadRemoteBundle(string url, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            LoadBundle(GetBundleInfoByUrl(url), onComplete, onError);
        }

        public void LoadRemoteOrCachedBundle(string url, uint version)
        {
            LoadBundle(GetBundleInfoByUrlCached(url, version, null), null, null);
        }

        public void LoadRemoteOrCachedBundle(string url, uint version, string cacheId)
        {
            LoadBundle(GetBundleInfoByUrlCached(url, version, cacheId), null, null);
        }

        public void LoadRemoteOrCachedBundle(string url, uint version, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            LoadBundle(GetBundleInfoByUrlCached(url, version, null), onComplete, onError);
        }

        public void LoadRemoteOrCachedBundle(string url, uint version, string cacheId, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            LoadBundle(GetBundleInfoByUrlCached(url, version, cacheId), onComplete, onError);
        }

        /*
         * Loading by Info.
         */

        public void LoadBundle(AssetBundleInfo info)
        {
            LoadBundle(info, null, null);
        }

        public void LoadBundle(AssetBundleInfo info, Action<AssetBundleInfo> onComplete, Action<AssetsException> onError)
        {
            Log.Debug(i => $"Load bundle: {i}", info);

            if (_bundles.TryGetValue(info.BundleId, out var infoAdded))
            {
                if (info != infoAdded)
                {
                    Log.Warn(i => $"Bundle with the same id already added. Replacing added info and updating it. BundleId: {i}", info.BundleId);

                    info.Update(infoAdded);

                    _bundles[info.BundleId] = info;
                }
            }
            else
            {
                Log.Debug(i => $"Bundle info registered. BundleId: {i}", info.BundleId);

                _bundles.Add(info.BundleId, info);
            }

            if (info.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleAlreadyLoaded, info.BundleId);

#if UNITY_EDITOR
            LogEditorBundleLoadRequested(info);
#endif

            Dispatcher.Dispatch(AssetsEvent.BundleLoadingStart, info);

            _agent.LoadAsync(info,
                             bundleInfo => _destroying || _destroyed ? null : _cacheController?.GetBundleCacheInfo(bundleInfo.CacheId),
                             (loadingFromCache, bundleInfo) =>
                             {
                                 if (_destroying || _destroyed)
                                     return;
                                 
                                 bundleInfo.SetLoadingFromCache(loadingFromCache);
                                 
                                 Dispatcher?.Dispatch(AssetsEvent.BundleLoadingProgress, bundleInfo);
                             },
                             bundleInfo =>
                             {
                                 if (_destroying || _destroyed)
                                     return;
                                 
                                 bundleInfo.SetCacheReset();

                                 _cacheController?.CleanBundleCacheInfo(bundleInfo.CacheId);
                             },
                             (bundleName, bundleInfo) =>
                             {
                                 if (_destroying || _destroyed)
                                     return;

                                 bundleInfo.SetCacheRecorded();
                                 
                                 _cacheController?.RecordCacheInfo(bundleInfo.CacheId, bundleName, bundleInfo.BundleUrl, bundleInfo.BundleVersion, bundleInfo.DownloadedBytes);
                             },
                             (bundleInfo, progress, downloadedBytes) =>
                             {
                                 if (_destroying || _destroyed)
                                     return;

                                 Log?.Debug((p, n) => $"Bundle progress: {p} {n}", progress, bundleInfo.ToString());

                                 bundleInfo.SetLoadingProgress(progress, downloadedBytes);

                                 Dispatcher?.Dispatch(AssetsEvent.BundleLoadingProgress, bundleInfo);
                             },
                             (bundleInfo, unityBundle) =>
                             {
                                 if (_destroying || _destroyed)
                                     return;

                                 Log?.Debug(n => $"Bundle loaded: {n}", bundleInfo.BundleId);

                                 SetBundleLoaded(bundleInfo, unityBundle);

#if UNITY_EDITOR
                                 LogEditorBundleLoaded(bundleInfo);
#endif

                                 onComplete?.Invoke(bundleInfo);
                                 Dispatcher?.Dispatch(AssetsEvent.BundleLoadingSuccess, bundleInfo);
                             },
                             (bundleInfo, exception) =>
                             {
                                 if (_destroying || _destroyed)
                                     return;

                                 Log?.Error(exception);

#if UNITY_EDITOR
                                 LogEditorBundleLoadFailed(bundleInfo, exception);
#endif

                                 SetBundleUnloaded(bundleInfo);

                                 onError?.Invoke(exception);
                                 Dispatcher?.Dispatch(AssetsEvent.BundleLoadingFail, bundleInfo, exception);
                             });

            info.SetLoading();
        }

        /*
         * Loading Aborting.
         */

        public void AbortBundleLoading(Enum identifier)
        {
            AbortBundleLoading(GetBundleStringId(identifier));
        }

        public void AbortBundleLoading(string identifier)
        {
            if (!TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle);

            AbortBundleLoading(info);
        }

        public void AbortBundleLoading(AssetBundleInfo info)
        {
            if (!_bundles.ContainsValue(info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle);

            if (info.IsLoaded)
            {
                Log.Warn(n => $"Bundle loaded: {n}", info.ToString());
                return;
            }

            info.SetAborted();
        }

        private void AbortLoadingBundles()
        {
            foreach (var bundle in _bundles.Values)
            {
                if (bundle.IsLoading)
                    bundle.SetAborted();
            }
        }

        /*
         * Unloading.
         */

        public void UnloadBundle(Enum identifier, bool unloadObjects)
        {
            UnloadBundle(GetBundleStringId(identifier), unloadObjects);
        }

        public void UnloadBundle(string identifier, bool unloadObjects)
        {
            if (!TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle, identifier);
            UnloadBundle(info, unloadObjects);
        }

        public void UnloadBundle(AssetBundleInfo bundleInfo, bool unloadObjects)
        {
            if (!bundleInfo.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleNotLoaded, bundleInfo.BundleId);

#if UNITY_EDITOR
            var editorReason = bundleInfo.EditorDebugReason;
#endif

            bundleInfo.Bundle.Unload(unloadObjects);
            SetBundleUnloaded(bundleInfo);

#if UNITY_EDITOR
            LogEditorBundleUnloaded(bundleInfo.BundleId, unloadObjects, editorReason);
#endif

            Log.Debug(n => $"Bundle unloaded: {n}", bundleInfo.BundleId);
        }

        public void UnloadAllBundles(bool unloadObjects)
        {
            for (var i = _bundlesLoaded.Count - 1; i >= 0; i--)
                UnloadBundle(_bundlesLoaded[i], unloadObjects);
        }

        public void UnloadAllBundles(bool unloadObjects, params Enum[] except)
        {
            var exceptStringIdentifiers = new List<string>(except.Length);
            foreach (var item in except)
                exceptStringIdentifiers.Add(GetBundleStringId(item));

            for (var i = _bundlesLoaded.Count - 1; i >= 0; i--)
            {
                var bundle = _bundlesLoaded[i];
                if (!exceptStringIdentifiers.Contains(bundle.BundleId))
                    UnloadBundle(bundle, unloadObjects);
            }
        }

        /*
         * Getting Bundle.
         */

        public AssetBundleInfo GetBundle(Enum identifier)
        {
            return GetBundle(GetBundleStringId(identifier));
        }

        public AssetBundleInfo GetBundle(string identifier)
        {
            if (!TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle, identifier);

            if (!info.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleNotLoaded, info.BundleId);

            return info;
        }

        public bool TryGetBundle(Enum identifier, out AssetBundleInfo info)
        {
            return TryGetBundle(GetBundleStringId(identifier), out info);
        }

        public bool TryGetBundle(string identifier, out AssetBundleInfo info)
        {
            return TryGetBundleInfo(identifier, false, false, out info);
        }

        /*
         * Assets.
         */

        public T GetAsset<T>(Enum identifier, string assetName) where T : UnityEngine.Object
        {
            return GetAsset<T>(GetBundleStringId(identifier), assetName);
        }

        public T GetAsset<T>(string identifier, string assetName) where T : UnityEngine.Object
        {
            if (!TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle, identifier);

            return GetAsset<T>(info, assetName);
        }

        public T GetAsset<T>(AssetBundleInfo info, string assetName) where T : UnityEngine.Object
        {
            if (!info.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleNotLoaded, $"Bundle: \"{info.BundleId}\" Asset: \"{assetName}\"");

            var asset = (T)info.Bundle.LoadAsset(assetName, typeof(T));
            if (asset == null)
                throw new AssetsException(AssetsExceptionType.AssetNotFound, $"Asset: \"{assetName}\" Bundle: \"{info.BundleId}\"");

            return asset;
        }

        public AssetBundleRequest LoadAssetAsync<T>(AssetBundleInfo info, string assetName) where T : UnityEngine.Object
        {
            if (!info.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleNotLoaded, $"Bundle: \"{info.BundleId}\" Asset: \"{assetName}\"");

            return info.Bundle.LoadAssetAsync(assetName, typeof(T));
        }

        public bool TryGetAsset<T>(Enum identifier, string assetName, out T asset) where T : UnityEngine.Object
        {
            return TryGetAsset(GetBundleStringId(identifier), assetName, out asset);
        }

        public bool TryGetAsset<T>(string identifier, string assetName, out T asset) where T : UnityEngine.Object
        {
            if (!TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                throw new AssetsException(AssetsExceptionType.UnknownBundle, identifier);

            return TryGetAsset(info, assetName, out asset);
        }

        public bool TryGetAsset<T>(AssetBundleInfo info, string assetName, out T asset) where T : UnityEngine.Object
        {
            if (!info.IsLoaded)
                throw new AssetsException(AssetsExceptionType.BundleNotLoaded, info.BundleId);

            try
            {
                asset = info.Bundle.LoadAsset<T>(assetName);
            }
            catch (Exception exception)
            {
                Log.Error($"Asset loading error: {exception}");

                asset = null;
            }

            return asset != null;
        }

        /*
         * Cache.
         */

        public bool CheckBundleCached(AssetBundleInfo info)
        {
            return info.CacheId != null && _cacheController?.GetBundleCacheInfo(info.CacheId) != null;
        }

        public ulong GetBundleCacheSizeByCacheId(string cacheId)
        {
            return _cacheController?.GetBundleCacheInfo(cacheId)?.BundleSizeBytes ?? 0;
        }

        public ulong GetCachedFilesSizeBytes()
        {
            #if UNITY_WEBGL
            return 0;

            #else

            ulong size = 0;

            var paths = new List<string>();

            Caching.GetAllCachePaths(paths);

            foreach (var path in paths)
            {
                var cache = Caching.GetCacheByPath(path);
                size += (ulong)cache.spaceOccupied;
            }

            return size;

            #endif
        }

        public void CleanCache()
        {
            #if !UNITY_WEBGL

            Caching.ClearCache();

            #endif

            _cacheController.CleanAllBundlesCacheInfo();
        }

        private void InitializeCache()
        {
            if (_cacheController != null)
                throw new AssetsException(AssetsExceptionType.CacheControllerAlreadyInitialised);

            _cacheController = InjectionBinder.Construct<AssetBundlesCacheController>(true);
        }

        private void DisposeCache()
        {
            if (_cacheController == null)
                return;

            _cacheController.Destroy(InjectionBinder, true);
            _cacheController = null;
        }

        /*
         * Bundle Info.
         */

        private AssetBundleInfo GetBundleInfoById(string identifier)
        {
            if (TryGetBundleInfo(identifier, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                return info;

            if ((BundleInfoSettings & AssetBundleInfoSetting.CreateMissingInfo) == AssetBundleInfoSetting.CreateMissingInfo)
                info = AssetBundleInfo.FromId(identifier);

            return info;
        }

        private AssetBundleInfo GetBundleInfoByUrl(string url)
        {
            if (TryGetBundleInfo(url, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                return info;

            if ((BundleInfoSettings & AssetBundleInfoSetting.CreateMissingInfo) == AssetBundleInfoSetting.CreateMissingInfo)
                info = AssetBundleInfo.FromUrl(url);

            return info;
        }

        private AssetBundleInfo GetBundleInfoByUrlCached(string url, uint version, string cacheId)
        {
            if (TryGetBundleInfo(url, BundleInfoRequestingEnabled, BundleInfoCachingEnabled, out var info))
                return info;

            if ((BundleInfoSettings & AssetBundleInfoSetting.CreateMissingInfo) == AssetBundleInfoSetting.CreateMissingInfo)
                info = AssetBundleInfo.FromUrlCached(url, version, cacheId);

            return info;
        }

        private bool TryGetBundleInfo(string identifier, bool request, bool cache, out AssetBundleInfo info)
        {
            if (_bundles.TryGetValue(identifier, out info))
            {
                Log.Debug(i => $"Bundle info found. {i}", info);
                return true;
            }

            if (_bundlesInfoCache.TryGetValue(identifier, out info))
            {
                Log.Debug(i => $"Bundle info found in info cache. {i}", info);
                return true;
            }

            if (request && OnBundleInfoRequest != null)
            {
                Log.Debug(i => $"Requesting bundle info: {i}", identifier);

                info = OnBundleInfoRequest.Invoke(identifier);

                if (info != null && cache)
                {
                    Log.Debug(i => $"Bundle info saved to cache. {i}", info);

                    _bundlesInfoCache.Add(identifier, info);
                }
            }

            return info != null;
        }

        /*
         * Helpers.
         */

        private string GetBundleStringId(Enum identifier)
        {
            if (_ids.TryGetValue(identifier, out var id))
                return id;

            if (OnBundleStringIdentifier != null)
                id = OnBundleStringIdentifier.Invoke(identifier);

            id ??= AssetBundleInfo.EnumToStringIdentifier(identifier);

            _ids.Add(identifier, id);

            return id;
        }

        private void SetBundleLoaded(AssetBundleInfo info, AssetBundle unityBundle)
        {
            info.SetLoaded(unityBundle);

            _bundlesLoaded.Add(info);

            if (!info.HasAtlases)
                return;

            foreach (var atlasesName in info.AtlasesNames)
                _bundleByAtlasId.Add(atlasesName, info);
        }

        private void SetBundleUnloaded(AssetBundleInfo info)
        {
            info.Clean();

            // _bundles.Remove(info.BundleId);
            _bundlesLoaded.Remove(info);

            if (!info.HasAtlases)
                return;

            foreach (var atlasesName in info.AtlasesNames)
                _bundleByAtlasId.Remove(atlasesName);
        }

#if UNITY_EDITOR
        private void LogEditorBundleLoadRequested(AssetBundleInfo info)
        {
            Log.Warn(i => $"Asset bundle trace. Load requested for \"{i.BundleId}\". Reason: {FormatEditorReason(i.EditorDebugReason)}. Loaded: {FormatEditorLoadedBundles()}.", info);
        }

        private void LogEditorBundleLoaded(AssetBundleInfo info)
        {
            Log.Warn(i => $"Asset bundle trace. Loaded \"{i.BundleId}\". Reason: {FormatEditorReason(i.EditorDebugReason)}. Loaded: {FormatEditorLoadedBundles()}.", info);
        }

        private void LogEditorBundleLoadFailed(AssetBundleInfo info, Exception exception)
        {
            Log.Warn((i, e) => $"Asset bundle trace. Failed to load \"{i.BundleId}\". Reason: {FormatEditorReason(i.EditorDebugReason)}. Error: {e.Message}. Loaded: {FormatEditorLoadedBundles()}.", info, exception);
        }

        private void LogEditorBundleUnloaded(string bundleId, bool unloadObjects, string reason)
        {
            Log.Warn((id, objects, value) => $"Asset bundle trace. Unloaded \"{id}\" (unloadObjects={objects}). Reason: {FormatEditorReason(value)}. Loaded: {FormatEditorLoadedBundles()}.", bundleId, unloadObjects, reason);
        }

        private string FormatEditorLoadedBundles()
        {
            CollectLoadedBundleIds(_editorLoadedBundleIdsBuffer);
            return FormatEditorBundleIds(_editorLoadedBundleIdsBuffer);
        }

        private static string FormatEditorReason(string reason)
        {
            return string.IsNullOrEmpty(reason) ? "unspecified" : reason;
        }

        private static string FormatEditorBundleIds(List<string> bundleIds)
        {
            if (bundleIds.Count == 0)
                return "[none]";

            var builder = new StringBuilder();
            builder.Append('[');

            for (var i = 0; i < bundleIds.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(bundleIds[i]);
            }

            builder.Append(']');
            return builder.ToString();
        }
#endif

        /*
         * Event handlers.
         */

        private SpriteAtlas OnAtlasRequested(string atlasId)
        {
            if (_destroying || _destroyed)
                return null;
            
            _bundleByAtlasId.TryGetValue(atlasId, out var bundle);

            if (bundle == null || !bundle.IsLoaded)
            {
                if (AtlasProcessingMode == AssetsAtlasProcessingMode.Strict)
                    throw new AssetsException(AssetsExceptionType.AtlasBundleNotLoaded, atlasId);
                return null;
            }

            var atlas = GetAsset<SpriteAtlas>(bundle, atlasId);
            if (atlas != null)
                return atlas;

            if (AtlasProcessingMode == AssetsAtlasProcessingMode.Strict)
                throw new AssetsException(AssetsExceptionType.AtlasNotFound, $"Atlas: \"{atlas}\" Bundle: \"{bundle}\"");

            return null;
        }
    }
}
