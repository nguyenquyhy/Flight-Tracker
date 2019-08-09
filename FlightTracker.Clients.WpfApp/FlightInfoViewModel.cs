using FlightTracker.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlightTracker.Clients.WpfApp
{
    public enum ConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Failed
    }

    public class FlightInfoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            //if ((storage == null && value != null) || !storage.Equals(value))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            //return false;
        }

        private ConnectionState simConnectionState = ConnectionState.Idle;
        public ConnectionState SimConnectionState { get { return simConnectionState; } set { SetProperty(ref simConnectionState, value); } }

        private ConnectionState webConnectionState = ConnectionState.Idle;
        public ConnectionState WebConnectionState { get { return webConnectionState; } set { SetProperty(ref webConnectionState, value); } }

        private string title;
        public string Title { get { return title; } set { SetProperty(ref title, value); } }

        private AircraftData aircraftData;
        public AircraftData AircraftData { get { return aircraftData; } set { SetProperty(ref aircraftData, value); } }

        private FlightPlan flightPlan;
        public FlightPlan FlightPlan { get { return flightPlan; } set { SetProperty(ref flightPlan, value); } }

        private FlightStatus flightStatus;
        public FlightStatus FlightStatus { get { return flightStatus; } set { SetProperty(ref flightStatus, value); } }

        public void Update(AircraftData aircraftData)
        {
            AircraftData = aircraftData;
        }

        public void Update(FlightPlan flightPlan)
        {
            FlightPlan = flightPlan;
        }

        public void Update(FlightStatus flightStatus)
        {
            FlightStatus = flightStatus;
        }
    }
}
