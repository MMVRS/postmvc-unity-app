using Build1.PostMVC.Unity.App.Mediation;
using Build1.PostMVC.Unity.App.Modules.UI.Popups.Animation;
using Build1.PostMVC.Unity.App.Modules.UI.Popups.Impl;
using UnityEngine;
using Event = Build1.PostMVC.Core.MVCS.Events.Event;

namespace Build1.PostMVC.Unity.App.Modules.UI.Popups
{
    public abstract class PopupViewDispatcher : UnityViewDispatcher, IPopupViewCanvas
    {
        public static readonly Event OnShown  = new(typeof(PopupViewDispatcher), nameof(OnShown));
        public static readonly Event OnHidden = new(typeof(PopupViewDispatcher), nameof(OnHidden));

        [SerializeField, Header("Parts Base")] private GameObject     overlay;
        [SerializeField]                       private RectTransform  content;
        [SerializeField]                       private GameObject     raycastBlocker;
        [SerializeField]                       private PopupAnimation animationObject;

        public PopupBase  Popup      { get; private set; }
        public GameObject GameObject => gameObject;

        public GameObject    Overlay        => overlay;
        public RectTransform Content        => content;
        public GameObject    RaycastBlocker => raycastBlocker;
        public Event         OnShow         => OnShown;
        public Event         OnHide         => OnHidden;

        public bool InputBlocked => raycastBlocker && raycastBlocker.activeSelf;
        public bool IsAnimating  { get; private set; }
        public bool IsShown      { get; private set; }

        private PopupController _controller;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (animationObject)
            {
                if (raycastBlocker)
                    raycastBlocker.SetActive(true);

                animationObject.AnimateShow(this, OnShownImpl);
                IsAnimating = true;
                return;
            }

            OnShownImpl();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            TryKillAnimations();
        }

        /*
         * Public.
         */

        internal void SetUp(IPopupController controller, PopupBase popup)
        {
            _controller = (PopupController)controller;

            Popup = popup;
        }

        public void Close()                 { _controller.Close(Popup, false); }
        public void Close(bool immediately) { _controller.Close(Popup, immediately); }
        public void CloseImmediately()      { _controller.Close(Popup, true); }

        internal void CloseImpl(bool immediately)
        {
            if (!immediately && animationObject)
            {
                if (IsAnimating)
                    return;

                if (raycastBlocker)
                    raycastBlocker.SetActive(true);

                animationObject.AnimateHide(this, OnHiddenImpl);
                IsAnimating = true;
                return;
            }

            OnHiddenImpl();
        }

        /*
         * Protected.
         */

        protected virtual void OnShownHandler()  { }
        protected virtual void OnHiddenHandler() { }

        /*
         * Private.
         */

        private void OnShownImpl()
        {
            if (raycastBlocker)
                raycastBlocker.SetActive(false);

            IsAnimating = false;
            IsShown = true;

            OnShownHandler();

            Dispatch(OnShown);
        }

        private void OnHiddenImpl()
        {
            IsAnimating = false;
            IsShown = false;

            TryKillAnimations();
            OnHiddenHandler();

            Dispatch(OnHidden);

            _controller.FinalizeClosing(Popup);
        }

        private void TryKillAnimations()
        {
            if (animationObject)
            {
                if (raycastBlocker)
                    raycastBlocker.SetActive(false);

                animationObject.KillAnimations(this);
                IsAnimating = false;
            }
        }
    }
}
