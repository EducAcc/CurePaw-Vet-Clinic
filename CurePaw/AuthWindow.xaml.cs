using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    public partial class AuthWindow : Window
    {
        private CurePawEntities _db = MainWindow.Db;
        private bool _isLoginMode;

        public User CurrentUser { get; set; }

        public AuthWindow(bool isLoginMode = false)
        {
            InitializeComponent();
            _isLoginMode = isLoginMode;

            ChangeFormMode();
        }

        private void RegisterUser(string login, string password)
        {
            string loginPattern = @"^[a-zA-Z0-9_.-]+${4,}";
            string passwordPattern = @"^[a-zA-Z0-9@#$%._-]{4,}$";

            string name = NameField.Text.Trim();
            string surname = SurnameField.Text.Trim();
            string patronymic = PatronymicField.Text.Trim();
            string email = EmailField.Text.Trim();
            string phone = Regex.Replace(PhoneField.Text.Trim(), @"\D", "");

            bool isLoginLegit = login != "" && Regex.IsMatch(login, loginPattern);
            bool isPassLegit = password != "" && Regex.IsMatch(password, passwordPattern);
            bool isNameLegit = name != "";
            bool isSurnameLegit = surname != "";
            bool isEmailLegit = email != "" && Regex.IsMatch(email, MainWindow.EmailPattern);
            bool isPhoneLegit = phone != "" && Regex.IsMatch(phone, MainWindow.PhonePattern);

            if (!isNameLegit || !isSurnameLegit)
            {
                MessageBox.Show("Заполните все поля", "Некорректные данные");
                return;
            }

            if (!isLoginLegit || !isPassLegit)
            {
                MessageBox.Show("Логин и пароль могут быть только латиницей и мин длиной в 4 символа", "Некорректные данные");
                return;
            }

            if (!isEmailLegit)
            {
                MessageBox.Show("Неверный формат электронной почты", "Неверный формат");
                return;
            }

            if (!isPhoneLegit)
            {
                MessageBox.Show("Неверный формат номера телефона", "Неверный формат");
                return;
            }

            User newUser = new User()
            {
                Name = name,
                Surname = surname,
                Patronymic = patronymic,
                Email = email,
                Phone = phone,
                Login = login,
                Password = password,
                Role = 1
            };

            try
            {
                _db.Users.Add(newUser);
                _db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка");
            }

            _isLoginMode = !_isLoginMode;
            ChangeFormMode();
        }

        private void ChangeFormMode()
        {
            LinkLabel.Content = _isLoginMode ? "Еще нет аккаунта?" : "Уже есть аккаунт?";

            Title = _isLoginMode ? "Авторизация" : "Регистрация";
            TitleLabel.Content = Title;

            //В этой панели логин и пароль, очищается всегда
            ClearPanel(LogUserData);

            RegUserData.Visibility = _isLoginMode ? Visibility.Collapsed : Visibility.Visible;
            PhoneField.Visibility = _isLoginMode ? Visibility.Collapsed : Visibility.Visible;
            PhoneLabel.Visibility = _isLoginMode ? Visibility.Collapsed : Visibility.Visible;

            if (_isLoginMode)
            {
                //Тут данные для регистрации, очищаются при переходе на форму авторизации
                ClearPanel(RegUserData);
            }

            Grid.SetColumn(LogUserData, _isLoginMode ? 0 : 2);
            Grid.SetColumnSpan(LogUserData, _isLoginMode ? 3 : 1);

        }

        private void ClearPanel(Panel panel)
        {
            foreach (var element in panel.Children)
            {
                if (element is TextBox textBox)
                {
                    textBox.Clear();
                }
            }
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginField.Text.Trim();
            string password = PasswordField.Text.Trim();

            CurrentUser = _db.Users.Where(x => x.Login == login).FirstOrDefault();

            if (CurrentUser == null && !_isLoginMode)
            {
                RegisterUser(login, password);
            }
            else if (CurrentUser != null && _isLoginMode)
            {
                if (CurrentUser.Password == password)
                {
                    MessageBox.Show("Вы вошли");
                    DialogResult = true;
                    Close();
                    return;
                }

                MessageBox.Show("Неверный пароль");
            }
            else
            {
                string message = _isLoginMode ? "Пользователь с таким логином не существует" :
                    "Пользователь с таким логином уже существует";
                MessageBox.Show(message, "Ошибка");
            }
        }

        private void ChangeFormBtn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isLoginMode = !_isLoginMode;
            ChangeFormMode();
        }
    }
}
