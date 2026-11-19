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
using static System.Collections.Specialized.BitVector32;

namespace Byke.Pages
{
    /// <summary>Список заказов. Менеджер - просмотр, администратор - добавление/редактирование/удаление.</summary>
    public partial class OrdersPage : Page
    {
        public OrdersPage()
        {
            InitializeComponent();
            btnAdd.Visibility = Session.AdminVisibility;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                lstOrders.ItemsSource = Core.Context.Orders.OrderBy(o => o.OrderId).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить заказы:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new OrderEditPage(null));
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as Orders;
            if (order != null) NavigationService.Navigate(new OrderEditPage(order));
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var order = ((FrameworkElement)sender).DataContext as Orders;
            if (order == null) return;

            if (MessageBox.Show("Удалить заказ № " + order.OrderId + "?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                Core.Context.OrderItem.RemoveRange(order.OrderItem.ToList()); // сначала позиции заказа
                Core.Context.Orders.Remove(order);
                Core.Context.SaveChanges();
                LoadOrders();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось удалить заказ:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}