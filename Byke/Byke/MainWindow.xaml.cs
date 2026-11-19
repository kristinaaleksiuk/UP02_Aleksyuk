using Byke.Pages;
using System;
using System.Collections.Generic;
using System.Data;
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
using static System.Collections.Specialized.BitVector32;

namespace Byke
{
    /// <summary>Окно авторизации: вход по логину/паролю из БД или вход гостем.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            imgLogo.Source = ImageHelper.LoadPath(System.IO.Path.Combine(ImageHelper.ImagesDir, "logo.png"));
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (login == "" || password == "")
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Users user = Core.Context.Users.FirstOrDefault(u => u.Login == login);

                // Пароль сравниваем с учётом регистра
                if (user == null || !string.Equals(user.Password, password, StringComparison.Ordinal))
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Session.UserId = user.Id;
                Session.FullName = user.FIO;
                Session.Role = user.role.Name;
                NavigationService.Navigate(new ProductsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подключиться к базе данных:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();
            NavigationService.Navigate(new ProductsPage());
        }
    }
}
