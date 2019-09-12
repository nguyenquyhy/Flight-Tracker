import { Configs, FlightData, AircraftData } from "./Models";

export default class ApiService {
    async getConfigs() {
        const response = await fetch('api/Configs');
        return await response.json() as Configs;
    }

    async getFlight(id: string) {
        const response = await fetch(`api/Flights/${id}`);
        return await response.json() as FlightData;
    }

    async getFlights(limit?: number) {
        let url = 'api/Flights';
        if (limit) url += '?limit=' + limit;
        const response = await fetch(url);
        return await response.json() as FlightData[];
    }

    async deleteFlight(id: string) {
        const response = await fetch(`api/Flights/${id}`, {
            method: 'delete'
        });
    }

    async getAircrafts() {
        let url = 'api/Aircrafts';
        const response = await fetch(url);
        return await response.json() as AircraftData[];
        
    }
}