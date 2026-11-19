using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Byke.Pages
{
    /// <summary>Добавление / редактирование заказа (только администратор).</summary>
    public partial class OrderEditPage : Page
    {
        private readonly Orders _order; // null - режим добавления
        private readonly ObservableCollection<OrderItemRow> _items = new ObservableCollection<OrderItemRow>();

        public OrderEditPage(Orders order)
        {
            InitializeComponent();
            _order = order;
            gridItems.ItemsSource = _items;

            try
            {
                cmbAddres.ItemsSource = Core.Context.Addres.OrderBy(a => a.Id).ToList();
                cmbUser.ItemsSource = Core.Context.Users.OrderBy(u => u.FIO).ToList();
                cmbStatus.ItemsSource = Core.Context.StatusOrder.OrderBy(s => s.Id).ToList();
                cmbProduct.ItemsSource = Core.Context.Tovar.OrderBy(t => t.Articyl).ToList();

                if (_order == null)
                {
                    txtHeader.Text = "Добавление заказа";
                    dpOrderDate.SelectedDate = DateTime.Today;
                    dpDeliveryDate.SelectedDate = DateTime.Today.AddDays(7);
                    cmbStatus.SelectedIndex = 0;
                }
                else
                {
                    // Данные копируются в поля; в объект они вернутся только при "Сохранить"
                    txtHeader.Text = "Редактирование заказа № " + _order.OrderId;
                    cmbAddres.SelectedValue = _order.AddresId;
                    cmbUser.SelectedValue = _order.UsersId;
                    cmbStatus.SelectedValue = _order.StatusOrderId;
                    dpOrderDate.SelectedDate = _order.OrderDate;
                    dpDeliveryDate.SelectedDate = _order.DeliveryDate;
                    txtCode.Text = _order.Code.ToString();
                    foreach (OrderItem item in _order.OrderItem)
                        _items.Add(new OrderItemRow { Articyl = item.Articyl, NameTovar = item.Tovar.NameTovar, Quantity = item.Quantity });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var product = cmbProduct.SelectedItem as Tovar;
            if (product == null)
            {
                MessageBox.Show("Выберите товар.", "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int qty;
            if (!int.TryParse(txtQty.Text.Trim(), out qty) || qty <= 0)
            {
                MessageBox.Show("Количество должно быть целым числом больше 0.", "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если товар уже есть в заказе - суммируем количество
            var existing = _items.FirstOrDefault(i => i.Articyl == product.Articyl);
            if (existing != null)
            {
                qty += existing.Quantity;
                _items.Remove(existing);
            }
            _items.Add(new OrderItemRow { Articyl = product.Articyl, NameTovar = product.NameTovar, Quantity = qty });
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = gridItems.SelectedItem as OrderItemRow;
            if (selected != null) _items.Remove(selected);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            int code;
            string error = null;
            if (cmbAddres.SelectedValue == null) error = "Выберите пункт выдачи.";
            else if (cmbUser.SelectedValue == null) error = "Выберите клиента.";
            else if (cmbStatus.SelectedValue == null) error = "Выберите статус заказа.";
            else if (dpOrderDate.SelectedDate == null || dpDeliveryDate.SelectedDate == null) error = "Укажите даты заказа и доставки.";
            else if (dpDeliveryDate.SelectedDate < dpOrderDate.SelectedDate) error = "Дата доставки не может быть раньше даты заказа.";
            else if (!int.TryParse(txtCode.Text.Trim(), out code)) error = "Код для получения должен быть целым числом.";
            else if (_items.Count == 0) error = "Добавьте в заказ хотя бы один товар.";

            if (error != null)
            {
                MessageBox.Show(error, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            code = int.Parse(txtCode.Text.Trim());

            try
            {
                bool isNew = _order == null;
                Orders o = isNew ? new Orders() : _order;
                o.OrderDate = dpOrderDate.SelectedDate.Value;
                o.DeliveryDate = dpDeliveryDate.SelectedDate.Value;
                o.AddresId = (int)cmbAddres.SelectedValue;
                o.UsersId = (int)cmbUser.SelectedValue;
                o.StatusOrderId = (int)cmbStatus.SelectedValue;
                o.Code = code;
                if (isNew) Core.Context.Orders.Add(o);

                // Синхронизация позиций заказа: лишние удаляем, существующие обновляем, новые добавляем
                var oldItems = o.OrderItem.ToList();
                foreach (OrderItem old in oldItems)
                    if (!_items.Any(i => i.Articyl == old.Articyl))
                        Core.Context.OrderItem.Remove(old);

                foreach (OrderItemRow row in _items)
                {
                    OrderItem found = oldItems.FirstOrDefault(x => x.Articyl == row.Articyl);
                    if (found != null) found.Quantity = row.Quantity;
                    else o.OrderItem.Add(new OrderItem { Articyl = row.Articyl, Quantity = row.Quantity });
                }

                Core.Context.SaveChanges(); // всё сохраняется одной транзакцией

                MessageBox.Show("Заказ сохранён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось сохранить заказ:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
