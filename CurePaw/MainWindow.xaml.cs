using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CurePaw
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        static public CurePawEntities Db = new CurePawEntities();

        // Ситуативные переменные
        #region SituationVars
        private ServiceCategory _selectedAdminCategory;
        private List<ServiceSelectionItem> _servicesList;
        private List<PetSelectionItem> _petsList;
        private List<AppointmentStatu> _statuses;
        private Pet _selectedPet;
        private User _selectedEmployee;
        private ServiceCategory _selectedCategory;
        private Service _selectedService;
        private User _appointmentEmployer;
        private Appointment _selectedAppointment;
        #endregion

        // Основные свойства и ограничения
        #region MainVars
        public User CurrentUser { get; set; } = new User();
        static public string EmailPattern { get; } = @"^[a-zA-Z0-9_.-]+@[a-zA-Z0-9_-]+\.[a-zA-Z]{2,}$";
        static public string PhonePattern { get; } = @"^[78]\d{10}$";
        public DateTime StartDate { get; } = DateTime.Now.Date;
        public DateTime EndDate { get; } = DateTime.Now.Date.AddMonths(1);
        #endregion

        // Списки для UI
        #region UIList
        public List<AppointmentStatu> StatusesList
        {
            get => _statuses;
            set
            {
                _statuses = value;
                OnPropertyChanged();
            }
        }

        public List<ServiceSelectionItem> ServicesList
        {
            get => _servicesList;
            set
            {
                _servicesList = value;
                OnPropertyChanged();
            }
        }

        public List<PetSelectionItem> PetsList
        {
            get => _petsList;
            set
            {
                _petsList = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            UpdateAllData();

            DataContext = this;
        }

        // Получение и возвращение данных из бд
        #region GetDataFunc
        private List<UserAppoinmentItem> GetUserAppoinments()
        {
            return Db.Appointments.Include(x => x.AppointmentStatu)
                .Where(x => x.Status != 3)
                .ToList()
                .Select(x => new UserAppoinmentItem { Appointment = x }).ToList();
        }

        private List<Pet> GetUserPets()
        {
            return Db.UserPets
               .Where(x => x.User.Id == CurrentUser.Id)
               .Select(j => j.Pet1)
               .ToList();
        }

        private List<User> GetUserEmployees()
        {
            return Db.Users
                .Where(x => x.Role == 2)
                .ToList();
        }

        private List<Appointment> GetAllAppointments()
        {
            if (CurrentUser.Role == 3)
            {
                return Db.Appointments.ToList();
            }
            else
            {
                // Фильтруем записи сотрудника, проверяя наличие его ID в оказании услуг по этой записи
                return Db.Appointments
                    .Where(x => x.Status == 1 && x.AppointmentServices.Any(aserv => aserv.EmployeeService.EmployeeId == CurrentUser.Id))
                    .ToList();
            }
        }
        #endregion

        // Обновление UI
        #region UpdateUI
        private void UpdateAllData()
        {
            ServicesTable.ItemsSource = Db.Services.ToList();
            EmployeeTable.ItemsSource = GetUserEmployees();
            AppointmentsTable.ItemsSource = GetAllAppointments();
            EmployeeCombo.ItemsSource = GetUserEmployees();
            UserAppointmentsTable.ItemsSource = GetUserAppoinments();

            StatusesList = Db.AppointmentStatus.ToList();

            CategoryFilterCombo.ItemsSource = Db.ServiceCategories.ToList();

            UpdateEmployeeServices();
            LoadPets();
            AdminCategoriesTable.ItemsSource = Db.ServiceCategories.ToList();
        }

        private void ChangeFormByUser()
        {
            UpdateAllData();

            AllAppointmentsPanel.Visibility = Visibility.Collapsed;
            AppointmentPanel.Visibility = Visibility.Collapsed;
            DashboardSideControl.Visibility = Visibility.Collapsed;
            DashboardTab.Visibility = Visibility.Visible;
            EmployeeTab.Visibility = Visibility.Visible;
            FinishAppointmentBtn.Visibility = Visibility.Collapsed;

            AddServiceBtn.Visibility = Visibility.Collapsed;
            AdminCategoryPanel.Visibility = Visibility.Collapsed;
            AddEmployeeBtn.Visibility = Visibility.Collapsed;

            MyTabControl.SelectedIndex = 0;

            AppointmentsTable.IsReadOnly = true;

            Grid.SetColumnSpan(UserDataPanel, 1);
            Grid.SetColumnSpan(ServicesTable, 1);

            switch (CurrentUser.Role)
            {
                case 2:
                    {
                        FinishAppointmentBtn.Visibility = Visibility.Visible;

                        AllAppointmentsPanel.Visibility = Visibility.Visible;
                        EmployeeTab.Visibility = Visibility.Collapsed;
                        break;
                    }
                case 3:
                    {
                        AllAppointmentsPanel.Visibility = Visibility.Visible;
                        AddServiceBtn.Visibility = Visibility.Visible;
                        AdminCategoryPanel.Visibility = Visibility.Visible;
                        AddEmployeeBtn.Visibility = Visibility.Visible;

                        AppointmentsTable.IsReadOnly = false;
                        Grid.SetColumnSpan(UserDataPanel, 3);

                        break;
                    }
                default:
                    {
                        AppointmentPanel.Visibility = Visibility.Visible;
                        DashboardSideControl.Visibility = Visibility.Visible;
                        break;
                    }
            }
        }

        private void UpdateEmployeeServices()
        {
            // Извлекаем только ветеринаров и связываем их с оказываемыми услугами
            var data = Db.Users
                .Where(x => x.Role == 2)
                .ToList()
                .Select(x => new EmployeeServicesItem
                {
                    Employee = x,
                    Services = Db.EmployeeServices
                        .Where(j => j.EmployeeId == x.Id)
                        .Select(c => c.Service1) // Навигационное свойство к самой услуге
                        .Distinct()
                        .ToList()
                }).ToList();

            CategoriesTable.ItemsSource = data;
        }

        private void LoadPets()
        {
            var allPets = GetUserPets();

            PetsList = allPets.Select(pet => new PetSelectionItem
            {
                Pet = pet,
                IsSelected = false
            }).ToList();
        }

        private void LoadServices()
        {
            if (_appointmentEmployer == null)
            {
                ServicesList = new List<ServiceSelectionItem>();
                return;
            }

            // Получаем только те услуги, которые оказывает данный сотрудник через EmployeeServices
            var employeeServices = Db.EmployeeServices
                .Where(x => x.EmployeeId == _appointmentEmployer.Id)
                .Select(j => j.Service1)
                .ToList();

            ServicesList = employeeServices.Select(service => new ServiceSelectionItem
            {
                Service = service,
                IsSelected = false
            }).ToList();
        }

        private bool TryDeleteTableRows<T>(DataGrid table, List<T> selectedItems = null) where T : class
        {
            if (!table.CurrentCell.IsValid)
            {
                return false;
            }

            MessageBoxResult result = MessageBox.Show(
                    "Удалить выбранные строки?",
                    "Подтверждение",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

            if (result != MessageBoxResult.OK)
            {
                return false;
            }

            try
            {
                var items = selectedItems ?? table.SelectedItems.Cast<T>();

                if (typeof(T) == typeof(UserAppoinmentItem))
                {
                    items.Cast<UserAppoinmentItem>().ToList().ForEach(x => x.Appointment.Status = 3);
                }
                else if (typeof(T) == typeof(Appointment))
                {
                    items.Cast<Appointment>().ToList().ForEach(x => x.Status = 3);
                }
                else
                {
                    Db.Set<T>().RemoveRange(items);
                }

                Db.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка");
            }

            return false;
        }

        #endregion

        // Диалоговые окна
        #region ShowDialog
        private void ShowAuthDialog(bool isLoginMode = false)
        {
            AuthWindow authWindow = new AuthWindow(isLoginMode);
            ShowDialog(authWindow);

            if (authWindow.DialogResult == true)
            {
                CurrentUser = authWindow.CurrentUser;
                ChangeFormByUser();

                DataContext = null;
                DataContext = this;

                PetTable.ItemsSource = GetUserPets();
                LoadPets();
            }
        }

        private void ShowPetDialog()
        {
            ShowDialog(new PetWindow(CurrentUser, _selectedPet));
            PetTable.ItemsSource = GetUserPets();
            LoadPets();
        }

        private void ShowEmployeeDialog()
        {
            ShowDialog(new EmployeeWindow(_selectedEmployee));
            EmployeeTable.ItemsSource = GetUserEmployees();
            EmployeeCombo.ItemsSource = GetUserEmployees();
        }

        private void ShowCategoryDialog()
        {
            ShowDialog(new CategoryWindow(_selectedCategory));
            UpdateEmployeeServices();
        }

        private void ShowServiceDialog()
        {
            ShowDialog(new ServiceWindow(_selectedService));
            UpdateServicesTable();
        }

        private void ShowDialog<T>(T window) where T : Window
        {
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void EditDialogData(object item, string notifyMessage, Action showWin)
        {
            if (item != null)
            {
                showWin();
                return;
            }

            MessageBox.Show(notifyMessage, "😧");
        }

        #endregion

        // Обработчики добавления новых объектов
        #region AddBtnEvents
        private void AddPetBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedPet = null;
            ShowPetDialog();
        }

        private void AddEmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedEmployee = null;
            ShowEmployeeDialog();
        }

        private void AddCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedCategory = null;
            ShowCategoryDialog();
        }

        private void AddServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedService = null;
            ShowServiceDialog();
        }
        #endregion

        // Различные нажатия на кнопки
        #region OtherClickEvents
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowAuthDialog(true);
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowAuthDialog();
        }

        private void CreateAppointmentBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeeCombo.SelectedItem as User;
            var selectedDate = AppointmentDatePicker.SelectedDate;

            var selectedPets = PetsList.Where(x => x.IsSelected).Select(x => x.Pet).ToList();
            var selectedServices = ServicesList.Where(x => x.IsSelected).Select(x => x.Service).ToList();

            if (!selectedPets.Any() || selectedEmployee == null || !selectedServices.Any())
            {
                MessageBox.Show("Заполните все поля, выберите питомцев и хотя бы одну услугу.", "Ошибка");
                return;
            }

            if (!(selectedDate >= StartDate && selectedDate <= EndDate))
            {
                MessageBox.Show("Неверная дата (Мин запись - за месяц, макс - через месяц)");
                return;
            }

            try
            {
                // Находим связь питомца с текущим пользователем в таблице UserPets (поскольку связь 1-к-1)
                var selectedPet = selectedPets.First();
                var userPet = Db.UserPets.FirstOrDefault(up => up.Client == CurrentUser.Id && up.pet == selectedPet.Id);
                if (userPet == null)
                {
                    MessageBox.Show("Выбранный питомец не связан с вашим профилем.", "Ошибка");
                    return;
                }

                var newAppointment = new Appointment
                {
                    PetId = userPet.Id,
                    Date = selectedDate.Value,
                    Status = 1
                };
                Db.Appointments.Add(newAppointment);

                // Привязываем услуги через таблицу EmployeeServices к AppointmentServices
                foreach (var service in selectedServices)
                {
                    var employeeService = Db.EmployeeServices.FirstOrDefault(es => es.EmployeeId == selectedEmployee.Id && es.service == service.Id);
                    if (employeeService != null)
                    {
                        var link = new AppointmentService
                        {
                            Appointment1 = newAppointment,
                            EmployeeService = employeeService
                        };
                        Db.AppointmentServices.Add(link);
                    }
                }

                Db.SaveChanges();

                MessageBox.Show("Данные сохранены", "Успешно");
                AppointmentsTable.ItemsSource = Db.Appointments.ToList();

                LoadPets();
                LoadServices();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка");
            }
        }

        private void SaveUserDataBtn_Click(object sender, RoutedEventArgs e)
        {
            string phone = Regex.Replace(CurrentUser.Phone.Trim(), @"\D", "");

            bool isEmailLegit = !string.IsNullOrWhiteSpace(CurrentUser.Email.Trim())
                && Regex.IsMatch(CurrentUser.Email.Trim(), EmailPattern);
            bool isPhoneLegit = !string.IsNullOrWhiteSpace(phone)
                && Regex.IsMatch(CurrentUser.Phone.Trim(), PhonePattern);
            bool isSurnameLegit = !string.IsNullOrWhiteSpace(CurrentUser.Surname.Trim());
            bool isNameLegit = !string.IsNullOrWhiteSpace(CurrentUser.Name.Trim());

            if (!isSurnameLegit || !isNameLegit)
            {
                MessageBox.Show("Заполните все поля", "Неверные данные");
                return;
            }

            if (!isEmailLegit)
            {
                MessageBox.Show("Неверный формат почты (Есть @ и домен '.ru', '.com' и тд)", "Неверный формат");
                return;
            }

            if (!isPhoneLegit)
            {
                MessageBox.Show("Неверный формат телефона (11 цифр, первая - 7 или 8)", "Неверный формат");
                return;
            }

            try
            {
                Db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
            }
        }

        private void FinishAppointmentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAppointment != null && _selectedAppointment.Status != 2)
            {
                try
                {
                    _selectedAppointment.Status = 2;
                    Db.SaveChanges();
                    UpdateAllData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при завершении записи: {ex.Message}");
                }
            }
        }

        private void RefToAddAppoinmentBtn_Click(object sender, RoutedEventArgs e)
        {
            MyTabControl.SelectedIndex = 0;
        }
        #endregion

        //Обработчики выбора элемента
        #region SelectionEvents
        private void PetTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPet = PetTable.SelectedItem as Pet;
        }

        private void EmployeeTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEmployee = EmployeeTable.SelectedItem as User;
        }

        private void ServicesTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedService = ServicesTable.SelectedItem as Service;
        }

        private void StatusTableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Db.SaveChanges();
        }

        private void EmployeeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _appointmentEmployer = EmployeeCombo.SelectedItem as User;
            LoadServices();
        }

        private void AppointmentsTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAppointment = AppointmentsTable.SelectedItem as Appointment;
        }
        #endregion

        // Обработчики для нажатия Delete
        #region PressDeleteEvents
        private void PetTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role == 2 || CurrentUser.Role == 3) return;

                bool isSuccessful = TryDeleteTableRows<Pet>(PetTable);

                if (!isSuccessful) e.Handled = true;

                PetTable.ItemsSource = GetUserPets();
            }
        }

        private void CategoriesTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Delete)
            //{
            //    if (CurrentUser.Role != 3) return;

            //    List<ServiceCategory> items = Db.ServiceCategories
            //                .RemoveRange(CategoriesTable.SelectedItems
            //                    .Cast<CategoryServicesItem>()
            //                    .Select(x => x.Category as ServiceCategory))
            //                    .ToList();

            //    bool isSuccessful = TryDeleteTableRows<ServiceCategory>(CategoriesTable, items);

            //    if (!isSuccessful) e.Handled = true;

            //    UpdateEmployeeServices();
            //}
        }

        private void EmployeeTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role != 3) return;

                bool isSuccessful = TryDeleteTableRows<User>(EmployeeTable);

                if (!isSuccessful) e.Handled = true;

                EmployeeTable.ItemsSource = GetUserEmployees();
            }
        }

        private void ServicesTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role != 3) return;

                bool isSuccessful = TryDeleteTableRows<Service>(ServicesTable);

                if (!isSuccessful) e.Handled = true;

                ServicesTable.ItemsSource = Db.Services.ToList();
            }
        }

        private void AppointmentsTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role != 3) return;

                bool isSuccessful = TryDeleteTableRows<Appointment>(AppointmentsTable);

                if (!isSuccessful) e.Handled = true;

                AppointmentsTable.ItemsSource = Db.Appointments.ToList();
            }
        }

        private void UserAppointmentsTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role != 1) return;

                bool isSuccessful = TryDeleteTableRows<UserAppoinmentItem>(UserAppointmentsTable);

                if (!isSuccessful) e.Handled = true;

                UserAppointmentsTable.ItemsSource = GetUserAppoinments();
            }
        }
        #endregion

        // Обработчики двойного клика ЛКМ
        #region MouseDoubleClickEvents
        private void ServicesTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser.Role != 3) return;

            EditDialogData(_selectedService, "Выберите услугу", ShowServiceDialog);
            ServicesTable.ItemsSource = Db.Services.ToList();
        }

        private void EmployeeTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser.Role != 3) return;

            EditDialogData(_selectedEmployee, "Выберите сотрудника", ShowEmployeeDialog);
        }

        private void CategoriesTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser.Role != 3) return;

            EditDialogData(_selectedCategory, "Выберите категорию", ShowCategoryDialog);
        }

        private void PetTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUser.Role != 1) return;

            EditDialogData(_selectedPet, "Выберите питомца", ShowPetDialog);
        }
        #endregion

        // Определяет событие и метод, которые требует интерфейс INotifyPropertyChange
        #region InterInitialization
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion


        // 1. Обработчик клика по строке: выводит имя выбранной категории в текстовое поле
        private void AdminCategoriesTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAdminCategory = AdminCategoriesTable.SelectedItem as ServiceCategory;
            if (_selectedAdminCategory != null)
            {
                CategoryNameField.Text = _selectedAdminCategory.Name;
            }
            else
            {
                CategoryNameField.Text = string.Empty;
            }
        }

        // 2. Обработчик кнопки сохранения (добавление или редактирование существующей категории)
        private void SaveCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            string name = CategoryNameField.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название категории.", "Ошибка");
                return;
            }

            try
            {
                if (_selectedAdminCategory == null)
                {
                    // Режим добавления новой категории
                    var newCategory = new ServiceCategory { Name = name };
                    Db.ServiceCategories.Add(newCategory);
                }
                else
                {
                    // Режим изменения существующей
                    _selectedAdminCategory.Name = name;
                }

                Db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успешно");

                // Сброс состояния формы
                CategoryNameField.Text = string.Empty;
                _selectedAdminCategory = null;
                AdminCategoriesTable.SelectedItem = null;

                // Обновление таблиц на UI
                AdminCategoriesTable.ItemsSource = Db.ServiceCategories.ToList();
                UpdateEmployeeServices(); // Перерасчет услуг сотрудников (если применимо)
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении категории: " + ex.Message, "Ошибка");
            }
        }

        // 3. Обработчик кнопки Delete для удаления категории из БД
        private void AdminCategoriesTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CurrentUser.Role != 3) return;

                // Используем универсальный метод удаления TryDeleteTableRows
                bool isSuccessful = TryDeleteTableRows<ServiceCategory>(AdminCategoriesTable);

                if (isSuccessful)
                {
                    CategoryNameField.Text = string.Empty;
                    _selectedAdminCategory = null;
                    AdminCategoriesTable.ItemsSource = Db.ServiceCategories.ToList();
                    UpdateEmployeeServices();
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        // 4. Двойной клик просто фокусирует курсор в текстовом поле
        private void AdminCategoriesTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedAdminCategory != null)
            {
                CategoryNameField.Focus();
            }
        }

        private void CategoriesTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAdminCategory = CategoriesTable.SelectedItem as ServiceCategory;
        }

        private void UpdateServicesTable()
        {
            try
            {
                // Подтягиваем услуги вместе с их категориями (ServiceCategory)
                var query = Db.Services.Include(s => s.ServiceCategory).AsQueryable();

                // 1. Поиск по названию
                string searchText = SearchField.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(x => x.Name.ToLower().Contains(searchText));
                }

                // 2. Фильтр по категории
                if (CategoryFilterCombo.SelectedItem is ServiceCategory selectedCategory)
                {
                    query = query.Where(x => x.Category == selectedCategory.Id);
                }

                ServicesTable.ItemsSource = query.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при фильтрации услуг: " + ex.Message);
            }
        }

        // Поиск при изменении текста
        private void SearchField_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateServicesTable();
        }

        // Фильтр при выборе категории
        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateServicesTable();
        }

        // Кнопка сброса фильтров
        private void ResetFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchField.Text = string.Empty;
            CategoryFilterCombo.SelectedItem = null;
            UpdateServicesTable();
        }
    }
}