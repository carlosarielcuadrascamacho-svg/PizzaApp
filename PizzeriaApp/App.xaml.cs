using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class App : Application
    {
        // Expose the application's IServiceProvider so pages can resolve services when needed
        public static IServiceProvider Services { get; set; }

        public App(AppShell shell)
        {
            InitializeComponent();
            MainPage = shell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}