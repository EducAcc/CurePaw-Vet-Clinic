using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class EmployeeWindow : Window, INotifyPropertyChanged
    {
        private CurePawEntities _db = MainWindow.Db;
        private User _currentUser;
        string _loginPattern = @"^[a-zA-Z0-9_.-]{4,}$";
        string _passwordPattern = @"^[a-zA-Z0-9@#$%._-]{4,}$";

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public EmployeeWindow(User currentUser = null)
        {
            CurrentUser = currentUser ?? new User();
            InitializeComponent();

            // ИЗМЕНЕНО: Загружаем список специальностей вместо категорий,
            // так как в новой БД сотрудник связан со специальностью (EmployeeSpeciality).
            CategoryCombo.ItemsSource = _db.EmployeeSpecialities.ToList();

            EmployeeCombo.ItemsSource = GetEmployers();
            DataContext = this;
        }

        private List<User> GetEmployers()
        {
            return _db.Users.Where(x => x.Role == 2).ToList();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // ИЗМЕНЕНО: Валидация заменена на проверку привязки к специальности (EmployeeSpeciality)
            if (string.IsNullOrWhiteSpace(CurrentUser.Name)
                || string.IsNullOrWhiteSpace(CurrentUser.Surname)
                || CurrentUser.EmployeeSpeciality == null)
            {
                MessageBox.Show("Заполните все поля (Отчество может быть пустым)",
                    "Некорректные данные");
                return;
            }

            if (!Regex.IsMatch(CurrentUser.Phone, MainWindow.PhonePattern))
            {
                MessageBox.Show("Неверный формат телефона (11 цифр, начиная с 7 или 8)",
                    "Неверный формат");
                return;
            }

            if (!Regex.IsMatch(CurrentUser.Email, MainWindow.EmailPattern))
            {
                MessageBox.Show("Неверный формат почты (Есть @ и домен '.ru' или '.com' и тд.)",
                    "Неверный формат");
                return;
            }

            if (!Regex.IsMatch(CurrentUser.Login, _loginPattern))
            {
                MessageBox.Show("Логин может содержать только латинские буквы и цифры (Мин 4)",
                    "Неверный формат");
                return;
            }

            if (!Regex.IsMatch(CurrentUser.Password, _passwordPattern))
            {
                MessageBox.Show("Пароль должен содержать только латниские буквы, спец символы и цифры (Мин 4)",
                    "Неверный формат");
                return;
            }

            if (CurrentUser.Id == 0)
            {
                _db.Users.Add(CurrentUser);
                _currentUser.Role = 2;
            }

            try
            {
                _db.SaveChanges();

                MessageBox.Show("Данные сохранены", "Успешно");

                EmployeeCombo.SelectedItem = null;
                EmployeeCombo.ItemsSource = GetEmployers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных: " + ex.Message, "Ошибка");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}