using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CurePaw
{
    public partial class ServiceWindow : Window, INotifyPropertyChanged
    {
        private CurePawEntities _db = MainWindow.Db;
        private Service _currentService;
        public Service CurrentService
        {
            get => _currentService;
            set
            {
                _currentService = value;
                OnPropertyChanged();
            }
        }

        public ServiceWindow(Service service)
        {
            CurrentService = service ?? new Service();
            InitializeComponent();

            // ИЗМЕНЕНО: Загружаем список категорий (ServiceCategory) вместо списка услуг,
            // чтобы пользователь мог связать услугу с конкретной категорией.
            // (Убедитесь, что в XAML имя ComboBox изменено на CategoryCombo, либо верните имя ServiceCombo)
            ServiceCombo.ItemsSource = _db.Services.ToList();
            CategoryCombo.ItemsSource = _db.ServiceCategories.ToList();
            DataContext = this;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentService.Name))
            {
                MessageBox.Show("Название не может быть пустым", "Ошибка", MessageBoxButton.OK);
                return;
            }

            // ИЗМЕНЕНО: Проверка стоимости сделана безопасной для не-nullable типов.
            // Если Price в модели сгенерирован как обычный double/decimal, проверка на null больше не вызовет ошибку.
            if (CurrentService.Price <= 0)
            {
                MessageBox.Show("Стоимость должна быть больше нуля", "Ошибка", MessageBoxButton.OK);
                return;
            }

            if (CurrentService.Id == 0)
            {
                _db.Services.Add(CurrentService);
            }

            try
            {
                _db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успех", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PriceField_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"\d");
        }
    }
}