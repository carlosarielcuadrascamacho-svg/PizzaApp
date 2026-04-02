using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class App : Application
    {
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