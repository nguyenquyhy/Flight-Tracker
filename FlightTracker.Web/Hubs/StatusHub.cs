using Microsoft.AspNetCore.SignalR;
using FlightTracker.DTOs;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Web.Hubs
{
    public class ConnectionInfo
    {
        public string Id { get; set; }
        public bool IsWeb { get; set; }
        public AircraftData AircraftData { get; set; }
        public FlightPlan FlightPlan { get; set; }
        public FlightStatus FlightStatus { get; set; }
    }

    public class StatusHub : Hub
    {
        private static ConcurrentDictionary<string, ConnectionInfo> connections = new ConcurrentDictionary<string, ConnectionInfo>();

        public override async Task OnConnectedAsync()
        {
            var id = this.Context.ConnectionId;
            connections.TryAdd(id, new ConnectionInfo { Id = id });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            connections.TryRemove(this.Context.ConnectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Ping()
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.IsWeb = true;
            }
            await Clients.Caller.SendAsync("List", connections.ToDictionary(o => o.Key, o => o.Value));
        }

        public async Task Set(AircraftData data)
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.AircraftData = data;

                await Clients.Clients(connections.Where(o => o.Value.IsWeb).Select(o => o.Key).ToList())
                    .SendAsync("List", connections.ToDictionary(o => o.Key, o => o.Value));
            }
        }

        public async Task Update(FlightStatus flightInfo)
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.FlightStatus = flightInfo;

                await Clients.All.SendAsync("Update", this.Context.ConnectionId, flightInfo);
            }
        }
    }
}
