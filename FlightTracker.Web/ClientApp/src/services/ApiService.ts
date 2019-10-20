import { Configs, FlightData, AircraftData, FlightStatus, FlightPlan } from "./Models";

type Partial<T> = {
    [P in keyof T]?: T[P];
}

const FlightSchema = `
id
title
description
startDateTime
endDateTime
airline
flightNumber
airportFrom
airportTo
aircraft {
    title
}
statusTakeOff {
    simTime
    localTime
    fuelTotalQuantity
    indicatedAirSpeed
}
statusLanding {
    simTime
    localTime
    fuelTotalQuantity
    indicatedAirSpeed
    verticalSpeed
}
videoUrl
state`

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
        flightNumber
        title
        airportFrom
        airportTo
        aircraft {
            title
        }
        statusTakeOff {
            localTime
        }
        statusLanding {
            localTime
        }
        route(last: 1) {
            localTime
        }
        state
        videoUrl
    }
}`

        const data = await this.graphQLquery(
            query,
            { last: limit }
        );
        return data.flights as FlightData[];
    }

    async getFlight(id: string) {
        const query = `query ($id: String!) { flight(id: $id) { ${FlightSchema} } }`

        const data = await this.graphQLquery(
            query,
            { id: id }
        );
        return data.flight as FlightData;
    }

    async getFlightPlan(id: string) {
        const query = `query ($id: String!) { 
    flight(id: $id) { 
        flightPlan {
            title
            cruisingAltitude
            departure {
                id
                name
                latitude
                longitude
                position
            }
            destination {
                id
                name
                latitude
                longitude
                position
            }
            waypoints {
                id
                type
                latitude
                longitude
                airway
            }
        }
    } 
}`

        const data = await this.graphQLquery(
            query,
            { id: id }
        );
        return data.flight.flightPlan as FlightPlan;
    }

    async getFlightRoute(id: string) {
        const query = `query ($id: String!) {
    flight(id: $id) {
        route {
            simTime
            latitude
            longitude
            altitude
            trueHeading
            screenshotUrl
            isOnGround
            isAutopilotOn
        }
    }
}`

        const data = await this.graphQLquery(
            query,
            { id: id }
        );
        return data.flight.route as FlightStatus[];
    }

    async patchFlight(id: string, flight: Partial<FlightData>) {
        const query = `mutation ($id: String!, $flight: PatchFlightInput!) { patchFlight(id: $id, flight: $flight) { ${FlightSchema} } }`

        const data = await this.graphQLquery(
            query,
            { id: id, flight: flight }
        );
        return data.patchFlight as FlightData;
    }

    async deleteFlight(id: string) {
        const query = `mutation($id: String!) { deleteFlight(id: $id) }`

        await this.graphQLquery(
            query,
            { id: id }
        );
    }

    async getAircrafts() {
        const query = `{
    aircrafts {
        tailNumber
        title
        model
        type
        pictureUrls(random: 7)
    }
}`

        const data = await this.graphQLquery(query);
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