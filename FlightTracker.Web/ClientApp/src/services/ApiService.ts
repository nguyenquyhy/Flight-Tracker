import { Configs, FlightData } from "./Models";

export default class ApiService {
    async getConfigs() {
        const response = await fetch('api/Configs');
        const config = await response.json() as Configs;
        return config;
    }

    async getFlight(id: string) {
        const response = await fetch(`api/Flights/${id}`);
        const data = await response.json() as FlightData;
        return data;
    }

    async getFlights(limit?: number) {
        let url = 'api/Flights';
        if (limit) url += '?limit=' + limit;
        const response = await fetch(url);
        const data = await response.json() as FlightData[];
        return data;
    }
}