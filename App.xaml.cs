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
            // Инициализируем Stores (используют Observer + Singleton паттерны)
            NavigationStore navigationStore = new NavigationStore();
            ProjectStore projectStore = new ProjectStore();
            AccountStore accountStore = new AccountStore();

            // Инициализируем сервисы (Proxy Pattern + Dependency Injection)
            // DatabaseService — реальный сервис работы с БД
            IDatabaseService databaseService = new DatabaseService();

            // DatabaseServiceProxy обёртывает DatabaseService для логирования (Proxy Pattern)
            IDatabaseService proxiedDatabaseService = new DatabaseServiceProxy(databaseService);

            // AuthorizationValidator проверяет права доступа (Strategy Pattern)
            IAuthorizationValidator authValidator = new AuthorizationValidator();

            // AuthorizationService использует DI для получения зависимостей (Dependency Injection)
            IAuthorizationService authService = new AuthorizationService(proxiedDatabaseService, authValidator);

            navigationStore.CurrentViewModel = new AuthorizationViewModel(navigationStore, projectStore, accountStore, authService, proxiedDatabaseService);

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(navigationStore)
            };

            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
