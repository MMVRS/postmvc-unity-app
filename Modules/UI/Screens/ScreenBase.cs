using System;

namespace Build1.PostMVC.Unity.App.Modules.UI.Screens
{
    public abstract class ScreenBase : UIControl<ScreenConfig>
    {
        public readonly Type              dataType;
        public readonly ScreenPrepareMode prepareMode;

        public bool AllowsPrepareWhenSingleInstance => prepareMode == ScreenPrepareMode.AllowWhenSingleInstance;
        
        protected ScreenBase(string name) : base(name)
        {
            prepareMode = ScreenPrepareMode.Default;
        }
        
        protected ScreenBase(string name, UIBehavior behavior) : base(name, behavior)
        {
            prepareMode = ScreenPrepareMode.Default;
        }

        protected ScreenBase(string name, UIBehavior behavior, ScreenPrepareMode prepareMode) : base(name, behavior)
        {
            this.prepareMode = prepareMode;
        }
        
        protected ScreenBase(string name, Type dataType) : base(name)
        {
            this.dataType = dataType;
            prepareMode = ScreenPrepareMode.Default;
        }
        
        protected ScreenBase(string name, UIBehavior behavior, Type dataType) : base(name, behavior)
        {
            this.dataType = dataType;
            prepareMode = ScreenPrepareMode.Default;
        }

        protected ScreenBase(string name, UIBehavior behavior, Type dataType, ScreenPrepareMode prepareMode) : base(name, behavior)
        {
            this.dataType = dataType;
            this.prepareMode = prepareMode;
        }
    }
}
