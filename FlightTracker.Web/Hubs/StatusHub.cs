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
        public FlightData FlightData { get; set; }
        public FlightStatus FlightStatus { get; set; }
    }

    public class StatusHub : Hub
    {
        private static ConcurrentDictionary<string, ConnectionInfo> connections = new ConcurrentDictionary<string, ConnectionInfo>();

        public override Task OnConnectedAsync()
        {
            var id = this.Context.ConnectionId;
            connections.TryAdd(id, new ConnectionInfo { Id = id });

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            connections.TryRemove(this.Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public Task List()
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.IsWeb = true;
            }
            return Clients.Caller.SendAsync("List", connections.ToDictionary(o => o.Key, o => o.Value));
        }

        public Task Set(FlightData data)
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.FlightData = data;

                var webClients = connections.Where(o => o.Value.IsWeb).Select(o => o.Key).ToList();

                return Task.WhenAll(
                    Clients.All.SendAsync("Update", this.Context.ConnectionId, null),
                    Clients.Clients(webClients).SendAsync("List", connections.ToDictionary(o => o.Key, o => o.Value))
                    );
            }
            return Task.CompletedTask;
        }

        public Task Update(FlightStatus flightInfo)
        {
            if (connections.TryGetValue(this.Context.ConnectionId, out var connectionInfo))
            {
                connectionInfo.FlightStatus = flightInfo;

                return Clients.All.SendAsync("Update", this.Context.ConnectionId, flightInfo);
            }
            return Task.CompletedTask;
        }
    }
}
