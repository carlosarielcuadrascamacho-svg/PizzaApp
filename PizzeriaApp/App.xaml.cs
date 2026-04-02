using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Resolvemos el Login desde el contenedor para que reciba sus dependencias
            var loginPage = serviceProvider.GetService<Login>();
            MainPage = new NavigationPage(loginPage);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}