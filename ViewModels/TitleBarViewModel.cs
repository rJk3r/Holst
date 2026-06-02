using Holst.Commands;
using System.Windows;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class TitleBarViewModel : ViewModelBase
    {
        public ICommand CloseWindowCommand { get; }
        public ICommand MinimizeWindowCommand { get; }
        public ICommand MaximizeWindowCommand { get; }

        public TitleBarViewModel()
        {
            CloseWindowCommand = new RelayCommand(() => Application.Current.MainWindow?.Close());

            MinimizeWindowCommand = new RelayCommand(() =>
            {
                if (Application.Current.MainWindow != null)
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });

            MaximizeWindowCommand = new RelayCommand(() =>
            {
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
            });
        }
    }
}
