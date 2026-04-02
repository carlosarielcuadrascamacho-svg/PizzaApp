using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new Views.Login());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}