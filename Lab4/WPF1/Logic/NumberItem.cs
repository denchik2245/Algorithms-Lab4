using System.ComponentModel;

namespace WPF1
{
    public class NumberItem : INotifyPropertyChanged
    {
        private int _value;
        private bool _isFinalized;
        private bool _isComparing;
        private double _xOffset; // Свойство для смещения по оси X

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public bool IsFinalized
        {
            get => _isFinalized;
            set
            {
                if (_isFinalized != value)
                {
                    _isFinalized = value;
                    OnPropertyChanged(nameof(IsFinalized));
                }
            }
        }

        public bool IsComparing
        {
            get => _isComparing;
            set
            {
                if (_isComparing != value)
                {
                    _isComparing = value;
                    OnPropertyChanged(nameof(IsComparing));
                }
            }
        }

        // Свойство для управления смещением по оси X
        public double XOffset
        {
            get => _xOffset;
            set
            {
                if (_xOffset != value)
                {
                    _xOffset = value;
                    OnPropertyChanged(nameof(XOffset));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}