using FlightTracker.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace FlightTracker.ClientTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/Hubs/Status")
                .Build();

            while (true)
            {
                try
                {
                    await client.StartAsync();
                    Console.WriteLine("Connected");

                    var lat = 1.3329081;
                    var lon = 103.7438207;

                    while (true)
                    {
                        await client.SendAsync("Update", new FlightStatus
                        {
                            Latitude = lat,
                            Longitude = lon,
                            Heading = 45
                        });
                        lat += 0.0005;
                        lon += 0.0005;
                        await Task.Delay(5000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.GetType().FullName} {ex.Message}!");
                }
            }
        }
    }
}
