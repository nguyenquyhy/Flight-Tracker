export interface Configs {
    googleMapsKey: string;
    permissions: { [id: string]: Permission }
}

export interface Permission {
    edit: boolean;
    delete: boolean;
}

export interface FlightData {
    id: string;
    title?: string;
    description?: string;

    airline: string;
    flightNumber: string;
    airportFrom: string;
    airportTo: string;

    aircraft?: AircraftData;
    flightPlan?: FlightPlan;

    startDateTime: string;

    statusTakeOff?: FlightStatus | null;
    statusLanding?: FlightStatus | null;
    statusCrash?: FlightStatus;

    fuelUsed: number;
    distanceFlown: number;

    route?: FlightStatus[];

    state: string;
}

export interface ClientData {
    isWeb: boolean;
    flightData?: FlightData;
    flightStatus?: FlightStatus;
}

export interface AircraftData {
    title: string;
    type: string;
    model: string;
    tailNumber: string;
    airline: string;
    flightNumber: string;

    pictureUrls: string[];
}

export interface FlightPlan {
    waypoints: Waypoint[];
}

export interface Waypoint {
    id: string;
    type: string;
    airway: string;
    latitude: number;
    longitude: number;
}

export interface FlightStatus {
    simTime: number;
    localTime?: number;

    latitude: number;
    longitude: number;
    altitude: number;

    heading: number;
    trueHeading: number;

    groundSpeed: number;
    indicatedAirSpeed: number;
    verticalSpeed: number;

    fuelTotalQuantity: number;

    pitch: number;
    roll: number;

    isOnGround: boolean;
    stallWarning: boolean;
    overspeedWarning: boolean;

    isAutopilotOn: boolean;

    screenshotUrl?: string;
}