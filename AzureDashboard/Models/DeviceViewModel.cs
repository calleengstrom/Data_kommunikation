
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AzureDashboard.Models
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string DeviceId { get; }

        private string _location = "";
        private string _receivedAt = "";
        private string _temperature = "-- °C";
        private string _humidity = "-- %";

        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged();
            }
        }

        public string ReceivedAt
        {
            get => _receivedAt;
            set
            {
                _receivedAt = value;
                OnPropertyChanged();
            }
        }

        public string Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged();
            }
        }

        public string Humidity
        {
            get => _humidity;
            set
            {
                _humidity = value;
                OnPropertyChanged();
            }
        }

        public DeviceViewModel(string deviceId)
        {
            DeviceId = deviceId;
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
