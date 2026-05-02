using Holst.Views;
using System.Windows;
using System.Windows.Input;

namespace Holst
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MoveWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && GeneralTitleBar.IsMouseOver)
            {
                this.DragMove();
            }

        }
    }
}