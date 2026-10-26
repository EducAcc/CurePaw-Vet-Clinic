using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace CurePaw
{
    public class UserAppoinmentItem : INotifyPropertyChanged
    {
        private Appointment _appoinment;
        private User _employee;
        private AppointmentStatu _status;

        // Использован оригинальный вызов string.Join поверх адаптированного свойства Pets
        // Изменена цепочка вызова к названию услуги: EmployeeService -> Service1 -> Name
        public string ServicesText
        {
            get => string.Join(", ", _appoinment?.AppointmentServices?.Select(x => x.EmployeeService?.Service1?.Name) ?? Array.Empty<string>());
        }

        public User Employee
        {
            get => _employee;
            set
            {
                _employee = value;
                OnPropertyChanged();
            }
        }

        public AppointmentStatu Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        // Возвращает список из одного питомца (так как в новой бд связь Appointment -> UserPet -> Pet1 стала 1-к-1)
        public Pet Pet
        {
            get
            {
                return _appoinment?.UserPet?.Pet1;
            }
        }

        public Appointment Appointment
        {
            get => _appoinment;
            set
            {
                _appoinment = value;

                // Получаем сотрудника из первой услуги, добавленной в запись
                Employee = _appoinment?.AppointmentServices?.FirstOrDefault()?.EmployeeService?.User;
                Status = _appoinment?.AppointmentStatu;

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}