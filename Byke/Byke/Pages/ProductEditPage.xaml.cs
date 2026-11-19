using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Xml.Linq;

namespace Byke.Pages
{
    /// <summary>Добавление / редактирование товара (только администратор).</summary>
    public partial class ProductEditPage : Page
    {
        private readonly Tovar _product; // null - режим добавления
        private string _photo;           // имя файла фото в папке images

        public ProductEditPage(Tovar product)
        {
            InitializeComponent();
            _product = product;

            try
            {
                cmbSuploer.ItemsSource = Core.Context.Suploer.OrderBy(x => x.Name).ToList();
                cmbManufacture.ItemsSource = Core.Context.Manufacture.OrderBy(x => x.Name).ToList();
                cmbCategory.ItemsSource = Core.Context.CategoryTovar.OrderBy(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (_product == null)
            {
                txtHeader.Text = "Добавление товара";
                txtDiscount.Text = "0";
                txtQuluety.Text = "0";
            }
            else
            {
                // Данные товара копируются в поля. В сам объект они вернутся только при "Сохранить",
                // поэтому "Отмена" ничего не портит.
                txtHeader.Text = "Редактирование товара";
                txtArticyl.Text = _product.Articyl;
                txtArticyl.IsEnabled = false; // артикул - первичный ключ
                txtName.Text = _product.NameTovar;
                txtPrice.Text = _product.Price.ToString("0.00", CultureInfo.InvariantCulture);
                cmbSuploer.SelectedValue = _product.SuploerId;
                cmbManufacture.SelectedValue = _product.ManufactureId;
                cmbCategory.SelectedValue = _product.CategoryTovarId;
                txtDiscount.Text = _product.Discount.ToString();
                txtQuluety.Text = _product.Quluety.ToString();
                txtDiscription.Text = _product.Discription;
                _photo = _product.Photo;
            }
            imgPhoto.Source = ImageHelper.Load(_photo);
        }

        /// <summary>Выбор нового фото: файл копируется в папку images рядом с exe.</summary>
        private void btnPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                Directory.CreateDirectory(ImageHelper.ImagesDir);
                string newName = Guid.NewGuid().ToString("N") + Path.GetExtension(dlg.FileName).ToLower();
                File.Copy(dlg.FileName, Path.Combine(ImageHelper.ImagesDir, newName));
                _photo = newName;
                imgPhoto.Source = ImageHelper.Load(_photo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить изображение:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string articyl = txtArticyl.Text.Trim();
            string name = txtName.Text.Trim();
            decimal price;
            int discount, quluety;

            // --- Проверка данных ---
            string error = null;
            if (articyl == "") error = "Укажите артикул.";
            else if (name == "") error = "Укажите наименование.";
            else if (!decimal.TryParse(txtPrice.Text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out price) || price < 0)
                error = "Цена должна быть неотрицательным числом.";
            else if (cmbSuploer.SelectedValue == null) error = "Выберите поставщика.";
            else if (cmbManufacture.SelectedValue == null) error = "Выберите производителя.";
            else if (cmbCategory.SelectedValue == null) error = "Выберите категорию.";
            else if (!int.TryParse(txtDiscount.Text.Trim(), out discount) || discount < 0 || discount > 100)
                error = "Скидка должна быть целым числом от 0 до 100.";
            else if (!int.TryParse(txtQuluety.Text.Trim(), out quluety) || quluety < 0)
                error = "Количество на складе должно быть целым неотрицательным числом.";

            if (error != null)
            {
                MessageBox.Show(error, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Значения уже проверены выше
            price = decimal.Parse(txtPrice.Text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
            discount = int.Parse(txtDiscount.Text.Trim());
            quluety = int.Parse(txtQuluety.Text.Trim());

            try
            {
                bool isNew = _product == null;
                if (isNew && Core.Context.Tovar.Any(t => t.Articyl == articyl))
                {
                    MessageBox.Show("Товар с таким артикулом уже существует.", "Дубликат", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Tovar t2 = isNew ? new Tovar { Articyl = articyl } : _product;
                t2.NameTovar = name;
                t2.Price = price;
                t2.SuploerId = (int)cmbSuploer.SelectedValue;
                t2.ManufactureId = (int)cmbManufacture.SelectedValue;
                t2.CategoryTovarId = (int)cmbCategory.SelectedValue;
                t2.Discount = (byte)discount;
                t2.Quluety = quluety;
                t2.Discription = txtDiscription.Text.Trim();
                t2.Photo = _photo;

                if (isNew) Core.Context.Tovar.Add(t2);
                Core.Context.SaveChanges();

                MessageBox.Show("Данные сохранены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack(); // возврат к списку; он обновится сам (событие Loaded)
            }
            catch (Exception ex)
            {
                DbHelper.DiscardChanges();
                MessageBox.Show("Не удалось сохранить данные:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
