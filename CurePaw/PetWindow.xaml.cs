using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace CurePaw
{
    public partial class PetWindow : Window, INotifyPropertyChanged
    {
        private CurePawEntities _db = MainWindow.Db;
        private User _currentUser;
        private Pet _currentPet;

        public Pet CurrentPet
        {
            get => _currentPet;
            set
            {
                if (_currentPet != value)
                {
                    _currentPet = value;
                    OnPropertyChanged();
                }
            }
        }

        public PetWindow(User user, Pet selectedPet = null)
        {
            InitializeComponent();

            _currentUser = user;
            CurrentPet = selectedPet ?? new Pet();

            UpdateBoxData();

            DataContext = this;
        }

        private void UpdateBoxData()
        {
            PetTypeCombo.ItemsSource = _db.PetTypes.ToList();
            PetCombo.ItemsSource = _db.UserPets
                .Where(x => x.User.Id == _currentUser.Id)
                .Select(j => j.Pet1)
                .ToList();
        }

        private void SavePetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentPet.Name)
                || CurrentPet.PetType == null)
            {
                MessageBox.Show("Заполните поля 'Кличка' и 'Тип'", "Некорректные данные");
                return;
            }

            if (CurrentPet.Id == 0)
            {
                _db.Pets.Add(CurrentPet);

                UserPet userPet = new UserPet
                {
                    User = _currentUser,
                    Pet1 = CurrentPet
                };

                _db.UserPets.Add(userPet);

                _db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успешно");
            }
            try
            {
                _db.SaveChanges();
                MessageBox.Show("Данные сохранены", "Успешно");

                PetCombo.SelectedItem = null;
                CurrentPet = new Pet();

                UpdateBoxData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных: " + ex.Message, "Ошибка");
            }
        }


        //Для реализации второго интерфейса.Обновляет UI, чтобы не выписывать все самому
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
