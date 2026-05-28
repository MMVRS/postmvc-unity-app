using System;
using System.Collections;
using Build1.PostMVC.Unity.App.Modules.Assets.Impl.Cache;
using Model.AssetBundles;
using UnityEngine;
using UnityEngine.Networking;

namespace Build1.PostMVC.Unity.App.Modules.Assets.Impl.Agents
{
    internal sealed class AssetsAgentWebGL : AssetsAgentBase
    {
        private const int    ManifestSchemaVersion = 1;
        private const string ManifestFileName      = "asset-bundles.json";

        private AssetBundlesManifestDto _manifest;
        private AssetsException         _manifestException;
        private bool                    _manifestLoading;

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
            {
                StartCoroutine(LoadEmbedAssetBundleFromManifestCoroutine(info, onProgress, onComplete, onError));
                return;
            }

            if (!info.IsRemoteBundle)
                throw new AssetsException(AssetsExceptionType.UnknownBundleType);

            StartCoroutine(LoadRemoteAssetBundleCoroutine(info, null, (_, __) => { }, null, null, onProgress, onComplete, onError));
        }

        private IEnumerator LoadEmbedAssetBundleFromManifestCoroutine(AssetBundleInfo info,
                                                                      Action<AssetBundleInfo, float, ulong> onProgress,
                                                                      Action<AssetBundleInfo, AssetBundle> onComplete,
                                                                      Action<AssetBundleInfo, AssetsException> onError)
        {
            yield return EnsureManifestLoaded();

            if (info.IsAborted)
            {
                onError.Invoke(info, new AssetsException(AssetsExceptionType.BundleLoadingAborted, info.BundleId));
                yield break;
            }

            if (_manifestException != null)
            {
                onError.Invoke(info, _manifestException);
                yield break;
            }

            if (!TryResolveBundleFile(info.BundleId, out var bundleFile))
            {
                onError.Invoke(info, new AssetsException(AssetsExceptionType.BundleManifestEntryNotFound, info.BundleId));
                yield break;
            }

            info.OverrideBundleUrl(CombineUrl(Application.streamingAssetsPath, bundleFile));
            yield return LoadRemoteAssetBundleCoroutine(info, null, (_, __) => { }, null, null, onProgress, onComplete, onError);
        }

        private IEnumerator EnsureManifestLoaded()
        {
            if (_manifest != null || _manifestException != null)
                yield break;

            if (_manifestLoading)
            {
                while (_manifestLoading)
                    yield return null;

                yield break;
            }

            _manifestLoading = true;

            var manifestUrl = CombineUrl(Application.streamingAssetsPath, ManifestFileName);
            var request = UnityWebRequest.Get(manifestUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                _manifestException = new AssetsException(AssetsExceptionType.BundleManifestLoadError, $"{manifestUrl}: {request.error}");
                _manifestLoading = false;
                yield break;
            }

            var manifestJson = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                _manifestException = new AssetsException(AssetsExceptionType.BundleManifestMalformed, $"{manifestUrl}: empty");
                _manifestLoading = false;
                yield break;
            }

            AssetBundlesManifestDto manifest = null;
            try
            {
                manifest = JsonUtility.FromJson<AssetBundlesManifestDto>(manifestJson);
            }
            catch (Exception exception)
            {
                _manifestException = new AssetsException(AssetsExceptionType.BundleManifestMalformed, $"{manifestUrl}: {exception.Message}");
            }

            if (_manifestException == null)
                _manifestException = ValidateManifest(manifest, manifestUrl);

            if (_manifestException == null)
                _manifest = manifest;

            _manifestLoading = false;
        }

        private static AssetsException ValidateManifest(AssetBundlesManifestDto manifest, string manifestUrl)
        {
            if (manifest == null)
                return new AssetsException(AssetsExceptionType.BundleManifestMalformed, $"{manifestUrl}: null");

            if (manifest.schemaVersion != ManifestSchemaVersion)
                return new AssetsException(AssetsExceptionType.BundleManifestUnsupportedVersion, $"{manifestUrl}: {manifest.schemaVersion}");

            if (manifest.bundles == null)
                return new AssetsException(AssetsExceptionType.BundleManifestMalformed, $"{manifestUrl}: bundles missing");

            for (var i = 0; i < manifest.bundles.Length; i++)
            {
                var bundle = manifest.bundles[i];
                if (bundle == null ||
                    string.IsNullOrWhiteSpace(bundle.id) ||
                    string.IsNullOrWhiteSpace(bundle.file) ||
                    string.IsNullOrWhiteSpace(bundle.sha256))
                    return new AssetsException(AssetsExceptionType.BundleManifestMalformed, $"{manifestUrl}: invalid bundle at index {i}");
            }

            return null;
        }

        private bool TryResolveBundleFile(string bundleId, out string bundleFile)
        {
            for (var i = 0; i < _manifest.bundles.Length; i++)
            {
                var bundle = _manifest.bundles[i];
                if (bundle.id == bundleId)
                {
                    bundleFile = bundle.file;
                    return true;
                }
            }

            bundleFile = null;
            return false;
        }

        private static string CombineUrl(string baseUrl, string file)
        {
            return baseUrl.EndsWith("/", StringComparison.Ordinal)
                       ? baseUrl + file
                       : baseUrl + "/" + file;
        }
    }
}
