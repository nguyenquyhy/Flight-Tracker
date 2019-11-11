using FlightTracker.DTOs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class FlightsAPIClient
    {
        private readonly string baseUrl;
        private readonly HttpClient httpClient;

        public FlightsAPIClient(IOptions<AppSettings> settings, HttpClient httpClient)
        {
            baseUrl = settings.Value.BaseUrl;
            this.httpClient = httpClient;
        }

        public async Task<FlightData> AddFlightAsync(FlightData data)
        {
            var result = await GraphQLAsync<dynamic>(@"mutation($flight: AddFlightInput!) {
    addFlight(flight: $flight) {
        id
        addedDateTime
    }
}", new { flight = new InputFlightData(data) });

            data.Id = result.addFlight.id;
            data.AddedDateTime = result.addFlight.addedDateTime;

            return data;
        }

        public async Task UpdateFlightAsync(string id, FlightData flight)
        {
            var result = await GraphQLAsync<dynamic>(@"mutation($id: String!, $flight: UpdateFlightInput!) {
    updateFlight(id: $id, flight: $flight) {
        id
    }
}", new { id, flight });
        }

        public async Task<double> PostRouteAsync(string id, List<FlightStatus> route)
        {
            if (route.Count == 0) return 0;

            var result = await GraphQLAsync<dynamic>(@"mutation($id: String!, $route: [FlightStatusInput]!) {
    appendRoute(id: $id, route: $route) {
        id
        route(last: 1) {
            simTime
        }
    }
}", new { id, route });

            return (double)result.appendRoute.route[0].simTime;
        }

        private async Task<T> GraphQLAsync<T>(string query, object variables = null)
        {
            var dataString = JsonConvert.SerializeObject(new { query, variables }, 
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), NullValueHandling = NullValueHandling.Ignore });
            var response = await httpClient.PostAsync(baseUrl + "/graphql", new StringContent(dataString, Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<GraphQLResult<T>>(responseString);
            if (result.Errors != null)
            {
                throw new Exception(string.Join(". ", result.Errors.Select(o => o.Message)));
            }

            return result.Data;
        }

        public class GraphQLResult<T>
        {
            public T Data { get; set; }
            public List<GraphQLError> Errors { get; set; }
        }

        public class GraphQLError
        {
            public string Message { get; set; }
            public GraphQLErrorExtensions Extensions { get; set; }
        }

        public class GraphQLErrorExtensions
        {
            public string Code { get; set; }
        }
    }
}
