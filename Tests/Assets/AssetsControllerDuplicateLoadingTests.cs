using System;
using System.Collections.Generic;
using System.Reflection;
using Build1.PostMVC.Core.MVCS.Events.Impl;
using Build1.PostMVC.Unity.App.Modules.Assets;
using Build1.PostMVC.Unity.App.Modules.Logging;
using NUnit.Framework;

namespace Build1.PostMVC.Unity.App.Tests.Assets
{
    public sealed class AssetsControllerDuplicateLoadingTests
    {
        [Test]
        public void LoadBundle_WhenBundleIdIsAlreadyLoading_ThrowsBeforeReplacingTrackedInfo()
        {
            var implementationType = typeof(IAssetsController).Assembly.GetType(
                "Build1.PostMVC.Unity.App.Modules.Assets.Impl.AssetsController",
                true);
            var controller = (IAssetsController)Activator.CreateInstance(implementationType, true);
            var trackedInfo = AssetBundleInfo.FromId("ui");
            var setLoadingMethod = typeof(AssetBundleInfo).GetMethod("SetLoading", BindingFlags.Instance | BindingFlags.NonPublic);
            setLoadingMethod.Invoke(trackedInfo, null);

            var bundlesField = implementationType.GetField("_bundles", BindingFlags.Instance | BindingFlags.NonPublic);
            var bundles = (Dictionary<string, AssetBundleInfo>)bundlesField.GetValue(controller);
            bundles.Add(trackedInfo.BundleId, trackedInfo);

            var joiningInfo = AssetBundleInfo.FromId("ui");
            var exception = Assert.Throws<AssetsException>(() => controller.LoadBundle(joiningInfo));

            Assert.AreEqual(AssetsExceptionType.BundleAlreadyLoading, exception.type);
            Assert.AreSame(trackedInfo, bundles[trackedInfo.BundleId]);
            Assert.IsTrue(trackedInfo.IsLoading);
            Assert.IsFalse(joiningInfo.IsLoading);
        }

        [Test]
        public void LoadBundle_WhenSameBundleReentersFromLoadingStart_MarksOriginalLoadingBeforeRejectingSecondStart()
        {
            var implementationType = typeof(IAssetsController).Assembly.GetType(
                "Build1.PostMVC.Unity.App.Modules.Assets.Impl.AssetsController",
                true);
            var controller = (IAssetsController)Activator.CreateInstance(implementationType, true);
            var dispatcher = new EventDispatcher();
            var logType = typeof(ILog).Assembly.GetType(
                "Build1.PostMVC.Unity.App.Modules.Logging.Impl.LogDefault",
                true);
            var log = (ILog)Activator.CreateInstance(logType, new object[] { null, LogLevel.None, null });
            implementationType.GetProperty("Dispatcher").SetValue(controller, dispatcher);
            implementationType.GetProperty("Log").SetValue(controller, log);

            var originalInfo = AssetBundleInfo.FromId("ui");
            var joiningInfo = AssetBundleInfo.FromId("ui");
            var loadingStartCount = 0;
            var originalWasLoadingDuringEvent = false;

            dispatcher.AddListener<AssetBundleInfo>(AssetsEvent.BundleLoadingStart, bundleInfo =>
            {
                loadingStartCount++;
                originalWasLoadingDuringEvent = bundleInfo.IsLoading;
                controller.LoadBundle(joiningInfo);
            });

            var exception = Assert.Throws<AssetsException>(() => controller.LoadBundle(originalInfo));

            var bundlesField = implementationType.GetField("_bundles", BindingFlags.Instance | BindingFlags.NonPublic);
            var bundles = (Dictionary<string, AssetBundleInfo>)bundlesField.GetValue(controller);

            Assert.AreEqual(AssetsExceptionType.BundleAlreadyLoading, exception.type);
            Assert.AreEqual(1, loadingStartCount);
            Assert.IsTrue(originalWasLoadingDuringEvent);
            Assert.AreSame(originalInfo, bundles[originalInfo.BundleId]);
            Assert.IsTrue(originalInfo.IsLoading);
            Assert.IsFalse(joiningInfo.IsLoading);
        }
    }
}
