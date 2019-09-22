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
        const query = `query ($last: Int) {
    flights(last: $last) {
        id
        startDateTime
        takeOffLocalTime
        landingLocalTime
        title
        airportFrom
        airportTo
        aircraft {
            title
        }
        state
    }
}`

        var data = await this.graphQLquery(
            query,
            { last: limit }
        );
        return data.flights as FlightData[];
    }

    async getFlight(id: string) {
        const query = `query ($id: String) {
    flight(id: $id) {
        id
        title
        description
        startDateTime
        endDateTime
        flightNumber
        airportFrom
        airportTo
        aircraft {
            title
        }
        takeOffLocalTime
        statusTakeOff {
            simTime
            fuelTotalQuantity
            indicatedAirSpeed
        }
        statusLanding {
            simTime
            fuelTotalQuantity
            indicatedAirSpeed
            verticalSpeed
        }
        state
    }
}`

        var data = await this.graphQLquery(
            query,
            { id: id }
        );
        return data.flight as FlightData;
    }

    async getFlightRoute(id: string) {
        const query = `query ($id: String) {
    flight(id: $id) {
        route {
            simTime
            latitude
            longitude
            screenshotUrl
            isOnGround
            isAutopilotOn
        }
    }
}`

        var data = await this.graphQLquery(
            query,
            { id: id }
        );
        return data.flight.route as FlightStatus[];
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
        const query = `{
    aircrafts {
        tailNumber
        title
        model
        type
        pictureUrls(limit: 7)
    }
}`

        var data = await this.graphQLquery(query);
        return data.aircrafts as AircraftData[];
    }

    private async graphQLquery(query: string, variables?: any) {
        var response = await fetch('graphql', {
            method: 'post',
            body: JSON.stringify({
                query: query,
                variables: variables
            }),
            headers: {
                'Content-Type': 'application/json'
            }
        })
        var data = await response.json();
        return data.data;
    }
}