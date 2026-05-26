using UnityEngine;

namespace Build1.PostMVC.Unity.App.Modules.UI.Popups
{
    public interface IPopupViewCanvas : IPopupView
    {
        GameObject    Overlay { get; }
        RectTransform Content { get; }
    }
}
