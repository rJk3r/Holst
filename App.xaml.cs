using Holst;
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

            navigationStore.CurrentViewModel = new HomeViewModel(navigationStore);

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(navigationStore)
            };

            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}