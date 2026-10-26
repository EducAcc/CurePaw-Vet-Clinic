using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CurePaw
{
    // Класс переименован в EmployeeServicesItem для отражения связки "Сотрудник - Услуги"
    public class EmployeeServicesItem
    {
        private User _employee;
        private List<Service> _services;

        public User Employee
        {
            get => _employee;
            set => _employee = value;
        }

        public List<Service> Services
        {
            get => _services;
            set
            {
                _services = value ?? new List<Service>();
            }
        }

        public string ServicesStr
        {
            get
            {
                if (_services != null && _services.Count > 0)
                {
                    return string.Join(", ", _services.Select(s => s.Name));
                }
                return "Нет услуг.";
            }
        }

        public class ServiceSelectionItem : INotifyPropertyChanged
        {
            private bool _isSelected;

            public Service Service { get; set; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }

            public string ServiceName => Service?.Name;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}