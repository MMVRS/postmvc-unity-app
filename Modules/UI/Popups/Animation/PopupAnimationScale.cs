using System;
using DG.Tweening;
using UnityEngine;

namespace Build1.PostMVC.Unity.App.Modules.UI.Popups.Animation
{
    [CreateAssetMenu(fileName = "PopupAnimationScale", menuName = "PostMVC/Popups/Animations/Scale", order = 1)]
    public sealed class PopupAnimationScale : PopupAnimation
    {
        [Header("Show"), SerializeField] private float showScaleFrom = 0.75F;
        [SerializeField]                 private float showScaleTo   = 1F;
        [SerializeField]                 private float showDuration  = 0.12F;
        [SerializeField]                 private Ease  showEasing    = Ease.OutBack;
        
        [Header("Hide"), SerializeField] private float hideScaleFrom = 1F;
        [SerializeField]                 private float hideScaleTo   = 0.9F;
        [SerializeField]                 private float hideDuration  = 0.05F;
        [SerializeField]                 private Ease  hideEasing    = Ease.OutSine;

        public override void AnimateShow(IPopupView view, Action onComplete)
        {
            var canvasView = GetCanvasView(view);
            canvasView.Content.localScale = new Vector3(showScaleFrom, showScaleFrom, showScaleFrom);
            canvasView.Content.DOScale(showScaleTo, showDuration)
                .OnComplete(onComplete.Invoke)
                .SetEase(showEasing);
        }

        public override void AnimateHide(IPopupView view, Action onComplete)
        {
            var canvasView = GetCanvasView(view);
            canvasView.Content.localScale = new Vector3(hideScaleFrom, hideScaleFrom, hideScaleFrom);
            canvasView.Content.DOScale(hideScaleTo, hideDuration)
                .SetEase(hideEasing)
                .OnComplete(onComplete.Invoke);
        }

        public override void KillAnimations(IPopupView view)
        {
            GetCanvasView(view).Content.DOKill(true);
        }

        private IPopupViewCanvas GetCanvasView(IPopupView view)
        {
            if (view is IPopupViewCanvas canvasView)
                return canvasView;

            var viewType = view?.GetType().Name ?? "null";
            throw new InvalidOperationException($"{nameof(PopupAnimationScale)} requires {nameof(IPopupViewCanvas)} but received {viewType}.");
        }
    }
}
