namespace GrabExpress_INTPROG
{
    public partial class App : Application
    {
        private readonly AppShell _shell;

        public App(AppShell shell)
        {
            InitializeComponent();
            _shell = shell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(_shell);

            window.Width = 360;
            window.Height = 640;

            return window;
        }
    }
}