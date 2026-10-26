using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
    public partial class CategoryWindow : Window, INotifyPropertyChanged
    {
        private CurePawEntities _db = MainWindow.Db;
        private ServiceCategory _currentCategory;
        private List<ServiceSelectionItem> _servicesList;
        private bool _isAddMode;

        public ServiceCategory CurrentCategory
        {
            get => _currentCategory;
            set
            {
                _currentCategory = value;
                OnPropertyChanged();

                LoadServicesForCurrentCategory();
            }
        }
        public List<ServiceCategory> CategoriesList { get; set; }
        public List<ServiceSelectionItem> ServicesList
        {
            get => _servicesList;
            set
            {
                _servicesList = value;
                OnPropertyChanged();
            }
        }
        public string SelectedServicesText
        {
            get
            {
                if (ServicesList == null || !ServicesList.Any(x => x.IsSelected))
                    return "Выберите услуги";

                var selectedNames = ServicesList
                    .Where(x => x.IsSelected)
                    .Select(x => x.Service.Name);

                return string.Join(", ", selectedNames);
            }
        }

        public CategoryWindow(ServiceCategory category = null)
        {
            InitializeComponent();

            CategoriesList = _db.ServiceCategories.ToList();

            CurrentCategory = category ?? new ServiceCategory();

            _isAddMode = CurrentCategory.Id == 0;

            DataContext = this;
        }

        private void LoadServicesForCurrentCategory()
        {
            if (CurrentCategory == null)
            {
                ServicesList = new List<ServiceSelectionItem>();
                OnPropertyChanged(nameof(SelectedServicesText));
                return;
            }

            var allServices = _db.Services.ToList();

            // ИЗМЕНЕНО: Проверяем связь напрямую по внешнему ключу Category у услуг
            var selectionList = allServices.Select(service => new ServiceSelectionItem
            {
                Service = service,
                IsSelected = service.Category == CurrentCategory.Id
            }).ToList();

            foreach (var item in selectionList)
            {
                item.OnSelectionChanged += () => OnPropertyChanged(nameof(SelectedServicesText));
            }

            ServicesList = new List<ServiceSelectionItem>(selectionList);
            OnPropertyChanged(nameof(SelectedServicesText));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCategory == null) return;

            if (string.IsNullOrWhiteSpace(CurrentCategory.Name)
                || ServicesList == null
                || !ServicesList.Any(x => x.IsSelected))
            {
                MessageBox.Show("Пожалуйста, заполните все поля и выберите услуги.", "Ошибка");
                return;
            }

            if (_isAddMode)
            {
                _db.ServiceCategories.Add(CurrentCategory);

                // Сначала сохраняем саму категорию, чтобы БД сгенерировала действительный Id
                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании категории: " + ex.Message, "Ошибка");
                    return;
                }
            }

            // ИЗМЕНЕНО: Обновляем внешний ключ Category в таблице Services
            try
            {
                foreach (var item in ServicesList)
                {
                    var serviceInDb = _db.Services.FirstOrDefault(s => s.Id == item.Service.Id);
                    if (serviceInDb != null)
                    {
                        if (item.IsSelected)
                        {
                            serviceInDb.Category = CurrentCategory.Id;
                        }
                        else if (serviceInDb.Category == CurrentCategory.Id)
                        {
                            // Если выбор снят, а ранее услуга принадлежала этой категории — сбрасываем её
                            serviceInDb.Category = null;
                        }
                    }
                }

                if (_isAddMode)
                {
                    CurrentCategory = null;
                }

                _db.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!", "Успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных: " + ex.Message, "Ошибка");
            }
        }

        private void ServicesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServicesCombo.SelectedItem != null)
            {
                ServicesCombo.SelectedItem = null;
            }
        }

        private void ServicesCombo_Loaded(object sender, RoutedEventArgs e)
        {
            var combo = sender as ComboBox;
            var textBox = combo?.Template.FindName("PART_EditableTextBox", combo) as TextBox;

            if (textBox != null)
            {
                textBox.IsHitTestVisible = false;
                textBox.Focusable = false;
            }
        }
    }

    public class ServiceSelectionItem : INotifyPropertyChanged
    {
        public Service Service { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();

                    // ИЗМЕНЕНО: Строка закомментирована во избежание синтаксической ошибки
                    // Чекбокс изменился, обновляет в UI его
                    OnSelectionChanged?.Invoke();
                }
            }
        }

        public string ServiceName { get => Service.Name; }

        public event Action OnSelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}