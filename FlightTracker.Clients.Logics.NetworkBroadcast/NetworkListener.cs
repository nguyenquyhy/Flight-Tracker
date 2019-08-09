using FlightTracker.DTOs;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics.NetworkBroadcast
{
    public class NetworkListener : IFlightStatusUpdater
    {
        private readonly UdpClient client;

        public FlightStatus currentInfo = null;

        public event EventHandler<FlightStatusUpdatedEventArgs> FlightStatusUpdated;

        public NetworkListener()
        {
            client = new UdpClient(49002, AddressFamily.InterNetwork);
        }

        public async Task StartAsync()
        {
            while (true)
            {
                var result = await client.ReceiveAsync();

                var data = Encoding.UTF8.GetString(result.Buffer);

                //XGPSPrepar3D v4,103.701,1.574,3046.8,317.99,45.9
                //XATTPrepar3D v4,-42.187,3.928,-1.788

                var tokens = data.Split(',');
                var type = tokens[0].Substring(0, 4);
                var software = tokens[0].Substring(4, tokens[0].Length - 4);

                switch (type)
                {
                    case "XGPS":
                        if (currentInfo == null) currentInfo = new FlightStatus();

                        currentInfo.Longitude = double.Parse(tokens[1]);
                        currentInfo.Latitude = double.Parse(tokens[2]);
                        currentInfo.Altitude = double.Parse(tokens[3]);
                        currentInfo.Heading = double.Parse(tokens[4]);
                        currentInfo.IndicatedAirSpeed = double.Parse(tokens[5]);

                        break;
                    case "XATT":
                        if (currentInfo == null) currentInfo = new FlightStatus();

                        currentInfo.TrueHeading = double.Parse(tokens[1]);
                        currentInfo.Pitch = double.Parse(tokens[2]);
                        currentInfo.Bank = double.Parse(tokens[3]);

                        break;
                }

                if (currentInfo != null) FlightStatusUpdated?.Invoke(this, new FlightStatusUpdatedEventArgs(currentInfo));
            }
        }
    }
}
