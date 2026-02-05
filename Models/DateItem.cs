using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNLose.Models
{
    public class DateItem : INotifyPropertyChanged
    {
        public DateTime Date { get; set; }

        public string Display => Date.ToString("MM/dd");

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background)));
            }
        }

        public Color Background => IsSelected ? Colors.Orange : Colors.LightGray;

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
