using System;
using System.Collections.Generic;
using Build1.PostMVC.Core.MVCS.Injection;
using Build1.PostMVC.Core.MVCS.Mediation;
using Build1.PostMVC.Unity.App.Modules.Assets;
using Build1.PostMVC.Unity.App.Modules.Device;
using Build1.PostMVC.Unity.App.Modules.UI.Layers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Build1.PostMVC.Unity.App.Modules.UI
{
    public abstract class UIControlsController<T, C> where T : UIControl<C>
                                                     where C : UIControlConfiguration
    {
        [Inject] protected IUILayersController UILayerController { get; set; }
        [Inject] protected IAssetsController   AssetsController  { get; set; }
        [Inject] protected IMediationBinder    MediationBinder   { get; set; }
        [Inject] protected IDeviceController   DeviceController  { get; set; }

        private readonly HashSet<UIControlConfiguration> _installedConfigs;

        protected UIControlsController()
        {
            _installedConfigs = new HashSet<UIControlConfiguration>();
        }

        /*
         * Public.
         */

        public void Initialize(IEnumerable<T> controls)
        {
            foreach (var control in controls)
            {
                var configuration = DeviceController.GetConfiguration(control);
                if (CheckConfigInstalled(configuration))
                    continue;

                InstallConfiguration(configuration);

                if (!control.ToPreInstantiate)
                    continue;

                var layerView = UILayerController.GetLayerView<Transform>(configuration.appLayerId);
                Instantiate(control, configuration, layerView, false);
            }
        }

        /*
         * Protected.
         */

        protected GameObject GetInstance(T control, UIControlOptions options)
        {
            return GetInstance(control, options, out var isNewInstance);
        }

        protected GameObject GetInstance(T control, UIControlOptions options, out bool isNewInstance)
        {
            return GetInstance(control, options, null, out isNewInstance);
        }

        protected GameObject GetInstance(T control, UIControlOptions options, Transform parent, out bool isNewInstance)
        {
            isNewInstance = false;
            if (control == null)
                return null;

            var configuration = DeviceController.GetConfiguration(control);
            if (!CheckConfigInstalled(configuration))
                InstallConfiguration(configuration);

            var activate = (options & UIControlOptions.Activate) == UIControlOptions.Activate;
            var layer = UILayerController.GetLayerView<Transform>(configuration.appLayerId);
            parent ??= layer;

            if (control.IsSingleInstance)
            {
                var instanceTransform = parent.Find(control.name);
                if (!instanceTransform && parent != layer)
                    instanceTransform = layer.Find(control.name);
                if (instanceTransform)
                {
                    if (instanceTransform.parent != parent)
                        instanceTransform.SetParent(parent, false);

                    if (activate)
                        instanceTransform.gameObject.SetActive(true);
                    return instanceTransform.gameObject;
                }
            }

            var instantiate = (options & UIControlOptions.Instantiate) == UIControlOptions.Instantiate;
            if (!instantiate)
                return null;

            isNewInstance = true;
            return Instantiate(control, configuration, parent, activate);
        }

        protected GameObject FindInstance(T control)
        {
            if (control == null)
                return null;
            
            var configuration = DeviceController.GetConfiguration(control);
            if (!CheckConfigInstalled(configuration))
                InstallConfiguration(configuration);

            var layer = UILayerController.GetLayerView<Transform>(configuration.appLayerId);
            
            var instanceTransform = layer.Find(control.name);
            if (instanceTransform)
                return instanceTransform.gameObject;

            return null;
        }

        protected bool Deactivate(T control)
        {
            return Deactivate(control, out var destroyed);
        }

        protected bool Deactivate(T control, out bool destroyed)
        {
            destroyed = false;

            if (control == null)
                return false;

            var configuration = DeviceController.GetConfiguration(control);
            var layer = UILayerController.GetLayerView(configuration.appLayerId);
            var view = layer.transform.Find(control.name);
            if (!view || !view.gameObject.activeSelf)
                return false;

            if (control.ToDestroyOnDeactivation)
            {
                Object.Destroy(view.gameObject);
                destroyed = true;
            }
            else
            {
                view.gameObject.SetActive(false);
            }

            return true;
        }

        protected GameObject Instantiate(T control, C configuration, Component parent, bool active)
        {
            GameObject prefab;
            if (configuration.bundleInfo != null)
            {
                prefab = AssetsController.GetAsset<GameObject>(configuration.bundleInfo, configuration.prefabName);
            }
            else if (configuration.bundleId != null)
            {
                prefab = AssetsController.GetAsset<GameObject>(configuration.bundleId, configuration.prefabName);
            }
            else
            {
                if (configuration.prefabName.Contains("."))
                {
                    var index = configuration.prefabName.LastIndexOf(".", StringComparison.Ordinal);
                    var path = configuration.prefabName[..index];
                    prefab = Resources.Load<GameObject>(path);
                }
                else
                {
                    prefab = Resources.Load<GameObject>(configuration.prefabName);
                }
            }

            if (!prefab)
                throw new Exception($"Control prefab not found: {configuration.prefabName}");
            
            prefab.SetActive(active);

            var instance = Object.Instantiate(prefab, parent.transform);
            instance.name = control.name;

            return instance;
        }

        protected bool CheckControlIsActive(T control)
        {
            if (control == null)
                return false;

            var configuration = DeviceController.GetConfiguration(control);
            var layer = UILayerController.GetLayerView(configuration.appLayerId);
            var view = layer.transform.Find(control.name);
            return view != null && view.gameObject.activeSelf;
        }

        protected bool CheckConfigInstalled(C configuration)
        {
            return _installedConfigs.Contains(configuration);
        }

        protected void InstallConfiguration(C configuration)
        {
            foreach (var binding in configuration)
            {
                if (binding.viewInterfaceType != null)
                    MediationBinder.Rebind(binding.viewType, binding.viewInterfaceType).To(binding.mediatorType);
                else
                    MediationBinder.Rebind(binding.viewType).To(binding.mediatorType);
            }

            _installedConfigs.Add(configuration);
        }
    }
}
