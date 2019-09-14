import { Configs, FlightData, AircraftData, FlightStatus } from "./Models";

type Partial<T> = {
    [P in keyof T]?: T[P];
}

export default class ApiService {
    async getConfigs() {
        const response = await fetch('api/Configs');
        return await response.json() as Configs;
    }

    async getFlights(limit?: number) {
        let url = 'api/Flights';
        if (limit) url += '?limit=' + limit;
        const response = await fetch(url);
        return await response.json() as FlightData[];
    }

    async getFlight(id: string) {
        const response = await fetch(`api/Flights/${id}`);
        return await response.json() as FlightData;
    }

    async getFlightRoute(id: string) {
        const response = await fetch(`api/Flights/${id}/Route`);
        return await response.json() as FlightStatus[];
    }

    async patchFlight(id: string, flight: Partial<FlightData>) {
        let url = `api/Flights/${id}`;
        const response = await fetch(url, {
            method: 'patch',
            body: JSON.stringify(flight),
            headers: {
                'Content-Type': 'application/json'
            }
        });
        return await response.json() as FlightData;
    }

    async deleteFlight(id: string) {
        const response = await fetch(`api/Flights/${id}`, {
            method: 'delete'
        });
        if (!response.ok) {
            throw new Error();
        }
    }

    async getAircrafts() {
        let url = 'api/Aircrafts';
        const response = await fetch(url);
        return await response.json() as AircraftData[];
        
    }
}