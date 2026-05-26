using Build1.PostMVC.Unity.App.Mediation;
using UnityEngine;

namespace Build1.PostMVC.Unity.App.Modules.UI.Popups
{
    public interface IPopupView : IUnityView
    {
        PopupBase  Popup      { get; }
        GameObject GameObject { get; }

        void Close();
        void Close(bool immediately);
        void CloseImmediately();
    }
}
