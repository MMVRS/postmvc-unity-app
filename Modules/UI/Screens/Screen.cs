namespace Build1.PostMVC.Unity.App.Modules.UI.Screens
{
    public sealed class Screen : ScreenBase
    {
        public Screen(string name) : base(name)
        {
        }
        
        public Screen(string name, UIBehavior behavior) : base(name, behavior)
        {
        }

        public Screen(string name, UIBehavior behavior, ScreenPrepareMode prepareMode) : base(name, behavior, prepareMode)
        {
        }
    }
    
    public sealed class Screen<T> : ScreenBase
    {
        public Screen(string name) : base(name, typeof(T))
        {
        }
        
        public Screen(string name, UIBehavior behavior) : base(name, behavior, typeof(T))
        {
        }

        public Screen(string name, UIBehavior behavior, ScreenPrepareMode prepareMode) : base(name, behavior, typeof(T), prepareMode)
        {
        }
    }
}
