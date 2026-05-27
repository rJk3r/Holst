using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Holst.Services;

namespace Holst.Views
{
    /// <summary>
    /// Логика взаимодействия для AccountView.xaml
    /// </summary>
    public partial class AuthorizationView : UserControl
    {
        private readonly IAuthorizationService _authService;

        public AuthorizationView()
        {
            InitializeComponent();
            _authService = new AuthorizationService("unused");
        }

        private void LoginTopBtn_Click(object sender, RoutedEventArgs e)
        {
            // Hide password confirmation for login mode
            PasswordCheck.Visibility = Visibility.Collapsed;
            PasswordCheckLabel.Visibility = Visibility.Collapsed;
            ProceedAuth.Content = "Войти";
        }

        private void RegisterTopBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show password confirmation for registration mode
            PasswordCheck.Visibility = Visibility.Visible;
            PasswordCheckLabel.Visibility = Visibility.Visible;
            ProceedAuth.Content = "Зарегистрироваться";
        }

        private async void ProceedAuth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var action = ProceedAuth.Content as string;
                if (action == "Зарегистрироваться")
                {
                    var result = await _authService.Register(Email.Text, Email.Text, Password.Text, PasswordCheck.Text);
                    MessageBox.Show(result.ToString());
                    return;
                }
            }
            catch
            {

            }
            // Login flow
            var account = await _authService.Login(Email.Text, Password.Text);
            if (account == null)
            {
                MessageBox.Show("Ошибка! Проверьте, правильно ли введены данные.");
                return;
            }

            MessageBox.Show($"Welcome, {account.Name}");
        }
    }
}
