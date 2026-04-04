using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App(IServiceProvider serviceProvider)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCe0x0WmFZfVhgdl9FaVZQQ2YuP1ZhSXxVdkFjW39cc31XQmFVWUZ9XEE=");

            InitializeComponent();

            Services = serviceProvider;

            var loginPage = Services.GetService<Login>();
            MainPage = new NavigationPage(loginPage);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage);
        }
    }
}