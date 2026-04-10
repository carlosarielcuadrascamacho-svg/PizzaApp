using PizzeriaApp.Views;

namespace PizzeriaApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(MenuClient), typeof(MenuClient));
        }
    }
}
