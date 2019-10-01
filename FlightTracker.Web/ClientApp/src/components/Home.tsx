import React, { Component } from 'react';
import styled, { css } from 'styled-components';
import { ClientData, FlightStatus, AircraftData, FlightData } from '../services/Models';
import * as signalR from "@microsoft/signalr";
import { statusToColor, statusToThickness } from './Flight';
import GooglePageWrapper from "./hoc/GooglePageWrapper";
import RecentFlights from './RecentFlights';

interface FlightWrapper {
    id: string;

    aircraft?: AircraftData;
    status?: FlightStatus;

    history: FlightStatus[];

    marker?: google.maps.Marker;
    path?: google.maps.Polyline;
    flightPlanPath?: google.maps.Polyline;
    flightPlanWaypoints?: google.maps.Marker[];
}

interface State {
    hasData: boolean;
    flightData?: FlightData;
}

class Home extends Component<any, State> {
    static displayName = Home.name;

    private mapElement?: HTMLDivElement;
    private hubConnection: signalR.HubConnection = new signalR.HubConnectionBuilder().withUrl("/Hubs/Status").build();

    private flights: { [id: string]: FlightWrapper } = {};
    private map?: google.maps.Map;

    public constructor(props) {
        super(props);

        this.state = {
            hasData: false
        }

        this.hubConnection.on("List", (connections: { [id: string]: ClientData }) => {
            if (this.map) {
                for (var id of Object.keys(connections)) {
                    if (!connections[id].isWeb) {
                        const client = connections[id];

                        if (client.flightData) {
                            // TODO: change to adapt to multiple flights
                            this.setState({ flightData: client.flightData });

                            if (!this.flights[id]) {
                                this.flights[id] = {
                                    id: id,
                                    history: []
                                }
                            }
                            const flightPlan = client.flightData.flightPlan;

                            if (flightPlan && flightPlan.waypoints) {
                                if (!this.flights[id].flightPlanPath) {
                                    this.flights[id].flightPlanPath = new google.maps.Polyline({
                                        path: flightPlan.waypoints.map(waypoint => ({ lat: waypoint.latitude, lng: waypoint.longitude })),
                                        geodesic: true,
                                        strokeColor: '#FF0000',
                                        strokeOpacity: 1.0,
                                        strokeWeight: 2,
                                        map: this.map
                                    });
                                }
                                if (!this.flights[id].flightPlanWaypoints) {
                                    const waypoints: google.maps.Marker[] = [];
                                    for (let i = 0; i < flightPlan.waypoints.length; i++) {
                                        const current = flightPlan.waypoints[i];
                                        waypoints.push(new google.maps.Marker({
                                            position: {
                                                lat: current.latitude, lng: current.longitude
                                            },
                                            label: current.id,
                                            map: this.map
                                        }));
                                    }
                                    this.flights[id].flightPlanWaypoints = waypoints;
                                }
                            }
                        }
                    }
                }
            }
        });

        this.hubConnection.on("Update", (id, flightStatus: FlightStatus | null) => {
            this.setState({ hasData: true })

            if (!this.flights[id]) {
                this.flights[id] = {
                    id: id,
                    history: []
                }
            }

            let flight = this.flights[id];

            if (flightStatus == null) {
                flight.status = undefined;
                flight.history = [];
            } else {
                let latitude = flightStatus.latitude;
                let longitude = flightStatus.longitude;
                let heading = flightStatus.heading;

                flight.status = flightStatus;
                flight.history.push(flightStatus);

                if (!this.map) {
                    this.initMap();
                }

                if (this.map) {
                    var pos = { lat: latitude, lng: longitude };
                    if (flight.marker) {
                        flight.marker.setPosition(pos);
                        flight.marker.setIcon({
                            path: "M448 336v-40L288 192V79.2c0-17.7-14.8-31.2-32-31.2s-32 13.5-32 31.2V192L64 296v40l160-48v113.6l-48 31.2V464l80-16 80 16v-31.2l-48-31.2V288l160 48z",
                            scale: 0.06,
                            anchor: new google.maps.Point(16 / 0.06, 16 / 0.06),
                            rotation: heading,
                            fillColor: '#0000bb',
                            fillOpacity: 0.8
                        });
                    } else {
                        flight.marker = new google.maps.Marker({
                            position: pos,
                            map: this.map,
                            icon: {
                                path: "M448 336v-40L288 192V79.2c0-17.7-14.8-31.2-32-31.2s-32 13.5-32 31.2V192L64 296v40l160-48v113.6l-48 31.2V464l80-16 80 16v-31.2l-48-31.2V288l160 48z",
                                scale: 0.06,
                                anchor: new google.maps.Point(16 / 0.06, 16 / 0.06),
                                rotation: heading,
                                fillColor: '#0000bb',
                                fillOpacity: 0.8
                            }
                        });

                        this.map.setCenter(pos);
                    }

                    if (!flight.path ||
                        (flight.history.length > 0 && statusToColor(flight.history[flight.history.length - 1]) !== statusToColor(flightStatus))
                    ) {
                        flight.path = new google.maps.Polyline({
                            path: flight.history.map(pastStatus => ({ lat: pastStatus.latitude, lng: pastStatus.longitude })),
                            geodesic: true,
                            strokeColor: statusToColor(flightStatus),
                            strokeOpacity: 0.8,
                            strokeWeight: statusToThickness(flightStatus),
                            map: this.map
                        });
                    } else {
                        flight.path.setPath(flight.history.map(pastStatus => ({ lat: pastStatus.latitude, lng: pastStatus.longitude })));
                    }
                }
            }
        });
    }

    initMap() {
        if (this.mapElement) {
            this.map = new google.maps.Map(this.mapElement, {
                zoom: 13,
                mapTypeId: google.maps.MapTypeId.TERRAIN
            });
        }
    }

    componentDidMount() {
        this.mapElement = document.getElementById('map') as HTMLDivElement;

        this.hubConnection.start().then(() => {
            this.hubConnection.invoke('List');
            setInterval(() => this.hubConnection.invoke('List'), 15000);

            setInterval(() => {
                if (this.map) {
                    //if (Object.keys(this.markers).length > 0) {
                    //    let pos = this.markers[Object.keys(this.markers)[0]].getPosition();
                    //    if (pos) {
                    //        this.map.setCenter(pos);
                    //    }
                    //}
                }
            }, 1000);
        }).catch(function (err) {
            return console.error(err.toString());
        });
    }

    public render() {
        return (
            <>
                {!this.state.hasData &&
                    <>
                        <h3>No Live Flight at the moment!</h3>

                        <RecentFlights />
                    </>
                }
                <StyledWrapper visible={this.state.hasData}>
                    <div id="map" style={{ width: '100%', height: '100%' }}></div>
                    {this.state.flightData &&
                        <StyledTitle><p>{this.state.flightData.title}</p></StyledTitle>
                    }
                </StyledWrapper>
            </>
        );
    }
}

const StyledWrapper = styled.div`
position: relative;
width: 100%;
height: 100%;
${props => props.visible || css`display: none;`}
`

const StyledTitle = styled.div`
position: absolute;
top: 0;
left: 0;
right: 0;
display: flex;
align-items: center;
justify-content: center

p {
font-size: 1.2em;
font-weight: bold;
background-color: #d6c86b;
padding: 10px 20px;
text-transform: uppercase;
border-bottom: 10px solid #000;
box-shadow: 3px 3px 3px #888888;
}
`

export default GooglePageWrapper(Home);