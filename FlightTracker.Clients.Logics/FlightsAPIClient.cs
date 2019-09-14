using FlightTracker.DTOs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class FlightsAPIClient
    {
        private const string Endpoint = "/api/Flights";
        private readonly string baseUrl;
        private readonly HttpClient httpClient;

        public FlightsAPIClient(IOptions<AppSettings> settings)
        {
            baseUrl = settings.Value.BaseUrl;
            httpClient = new HttpClient();
        }

        public async Task<FlightData> PostAsync(FlightData data)
        {
            var dataString = JsonConvert.SerializeObject(data);
            var response = await httpClient.PostAsync(baseUrl + Endpoint, new StringContent(dataString, Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<FlightData>(responseString);
            return result;
        }

        public async Task<FlightData> PutAsync(string id, FlightData data)
        {
            var dataString = JsonConvert.SerializeObject(data);
            var response = await httpClient.PutAsync(baseUrl + Endpoint + "/" + id, new StringContent(dataString, Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<FlightData>(responseString);
            return result;
        }

        public async Task PostRouteAsync(string id, List<FlightStatus> route)
        {
            var dataString = JsonConvert.SerializeObject(route);
            var response = await httpClient.PostAsync(baseUrl + Endpoint + "/" + id + "/Route", new StringContent(dataString, Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        }
    }
}
