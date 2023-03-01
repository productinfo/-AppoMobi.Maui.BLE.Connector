using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppoMobi.Maui.BLE.Connector
{
    public class BleDeviceViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private string _Id;
        public string Id
        {
            get { return _Id; }
            set
            {
                if (_Id != value)
                {
                    _Id = value;
                    OnPropertyChanged();
                }
            }
        }


        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set
            {
                if (_Description != value)
                {
                    _Description = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _Rssi;
        public int Rssi
        {
            get
            {
                return _Rssi;
            }
            set
            {
                if (_Rssi != value)
                {
                    _Rssi = value;
                    OnPropertyChanged();
                }
            }
        }


        private ConnectionState _state;
        public ConnectionState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        public AppoMobi.Maui.BLE.Device Device { get; set; }

        /// <summary>
        /// This is filled ONLY when device is connected
        /// </summary>
        public ObservableCollection<BleServiceViewModel> Services { get; } = new ObservableCollection<BleServiceViewModel>();

    }
}
