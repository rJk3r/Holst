using Holst;
using Holst.Services;
using Holst.Stores;
using Holst.ViewModels;
using System.Windows;

namespace Holst
{
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            NavigationStore navigationStore = new NavigationStore();
            ProjectStore projectStore = new ProjectStore();
            AccountStore accountStore = new AccountStore();
            IAuthorizationService authService = new AuthorizationService("unused");

            navigationStore.CurrentViewModel = new AuthorizationViewModel(navigationStore, projectStore, accountStore, authService);

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(navigationStore)
            };

            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}