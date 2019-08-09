import React, { Component } from "react";
import { RouteComponentProps } from "react-router";
import SimpleLightBox from "simple-lightbox";
import "simple-lightbox/dist/simpleLightbox.css";
import GooglePageWrapper from "./hoc/GooglePageWrapper";
import { FlightData, FlightStatus } from "../services/Models";
import { ServicesContext } from "../Context";

interface State {
    loading: boolean;
    flight: FlightData | null;
}

interface RouteProps {
    id: string;
}

type Props = RouteComponentProps<RouteProps>;

class Flight extends Component<Props, State> {
    static displayName = Flight.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props) {
        super(props);
        this.state = { flight: null, loading: true };
    }

    componentDidMount() {
        this.populateData();
    }

    public render() {
        if (this.state.loading) return <p><em>Loading...</em></p>;

        if (!this.state.flight) return <p><strong>Cannot load flight</strong></p>;

        return <>
            <h2>{this.state.flight.title}</h2>
            <table className="table table-strip">
                <thead>
                    <tr>
                        <th>Information</th><th>Value</th>
                    </tr>
                </thead>
                <tbody>
                    <tr><td>From</td><td>{this.state.flight.airportFrom}</td></tr>
                    <tr><td>To</td><td>{this.state.flight.airportTo}</td></tr>
                    {this.state.flight.aircraft && <tr><td>Aircraft</td><td>{this.state.flight.aircraft.title}</td></tr>}
                    <tr><td>Flight Number</td><td>{this.state.flight.airline} {this.state.flight.flightNumber}</td></tr>
                    <tr><td>State</td><td>{this.state.flight.state}</td></tr>
                    {this.state.flight.statusTakeOff && this.state.flight.statusLanding ?
                        <>
                            <tr><td>Take-off/Landing Time</td><td>{Flight.secondToTime(this.state.flight.takeOffLocalTime)} - {Flight.secondToTime(this.state.flight.takeOffLocalTime + this.state.flight.statusLanding.simTime - this.state.flight.statusTakeOff.simTime)}</td></tr>
                            <tr><td>Full Duration</td><td>{Flight.formatDuration(this.state.flight.statusLanding.simTime - this.state.flight.statusTakeOff.simTime)}</td></tr>
                            <tr><td>Fuel Used</td><td>{Math.round((this.state.flight.statusTakeOff.fuelTotalQuantity - this.state.flight.statusLanding.fuelTotalQuantity) * 10) / 10} lb</td></tr>
                        </> :
                        <>
                            <tr><td>Take-off Time</td><td>{Flight.secondToTime(this.state.flight.takeOffLocalTime)}</td></tr>
                        </>
                    }
                    {this.state.flight.statusTakeOff && <tr><td>Take-off IAS</td><td>{this.state.flight.statusTakeOff.indicatedAirSpeed}</td></tr>}
                    {this.state.flight.statusLanding &&
                        <>
                            <tr><td>Landing IAS</td><td>{this.state.flight.statusLanding.indicatedAirSpeed}</td></tr>
                            <tr><td>Landing VS</td><td>{this.state.flight.statusLanding.verticalSpeed}</td></tr>
                        </>
                    }
                </tbody>
            </table>
            <p><em>*Blue: on the ground. Purple: autopilot off. Green: autopilot on</em></p>
            <div id="map" style={{ width: '100%', height: 800 }}></div>
        </>
    }

    async populateData() {
        const data = await this.context.api.getFlight(this.props.match.params.id);
        this.setState({ flight: data, loading: false });

        if (data.statuses && data.statuses.length > 0) {
            let mapElement = document.getElementById('map') as HTMLDivElement;

            let north = data.statuses.reduce((prev, curr) => Math.max(prev, curr.latitude), -1000);
            let south = data.statuses.reduce((prev, curr) => Math.min(prev, curr.latitude), 1000);
            let east = data.statuses.reduce((prev, curr) => Math.max(prev, curr.longitude), -1000);
            let west = data.statuses.reduce((prev, curr) => Math.min(prev, curr.longitude), 1000);

            let extra = 2;
            let map = new google.maps.Map(mapElement, {
                zoom: 1,
                center: {
                    lat: (north + south) / 2,
                    lng: (west + east) / 2
                },
                restriction: {
                    latLngBounds: {
                        north: north + extra,
                        south: south - extra,
                        east: east + extra,
                        west: west - extra
                    },
                    strictBounds: false
                },
                mapTypeId: google.maps.MapTypeId.TERRAIN
            });

            map.fitBounds({
                north: north,
                south: south,
                east: east,
                west: west
            })

            let infoWindow = new google.maps.InfoWindow();

            let arr: FlightStatus[] = [];
            let path: google.maps.Polyline | null = null;

            let allScreenshots = data.statuses.filter(o => o.screenshotUrl).map(o => o.screenshotUrl);

            for (let status of data.statuses) {
                if (path === null
                    || (arr.length > 0 && statusToColor(arr[arr.length - 1]) !== statusToColor(status))
                ) {
                    if (path !== null) {
                        path.setPath(arr.map(s => ({ lat: s.latitude, lng: s.longitude })))
                    }

                    arr = [];
                    path = new google.maps.Polyline({
                        path: [],
                        geodesic: true,
                        strokeColor: statusToColor(status),
                        strokeOpacity: 0.8,
                        strokeWeight: statusToThickness(status),
                        map: map
                    });
                }

                let pos = { lat: status.latitude, lng: status.longitude };

                if (data.statusTakeOff && status.simTime === data.statusTakeOff.simTime) {
                    new google.maps.Marker({
                        position: pos,
                        icon: {
                            path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
                            scale: 4,
                            anchor: new google.maps.Point(0, 5)
                        },
                        title: 'Took off',
                        map: map
                    });
                } else if (data.statusLanding && status.simTime === data.statusLanding.simTime) {
                    new google.maps.Marker({
                        position: pos,
                        icon: {
                            path: google.maps.SymbolPath.BACKWARD_CLOSED_ARROW,
                            scale: 4
                        },
                        title: 'Landed',
                        map: map
                    });
                }

                if (status.screenshotUrl) {
                    let marker = new google.maps.Marker({
                        position: pos,
                        //icon: {
                        //    url: status.screenshotUrl,
                        //    size: new google.maps.Size(48, 27),
                        //},
                        title: 'Picture',
                        map: map
                    });

                    let url = status.screenshotUrl;
                    google.maps.event.addListener(marker, 'mouseover', () => {
                        infoWindow.setContent(`<div><a target='_blank' href='${url}'><img width='100%' src='${url}' /></a></div>`);
                        infoWindow.open(map, marker);
                    })
                    google.maps.event.addListener(marker, 'mouseout', () => {
                        infoWindow.close();
                    });
                    google.maps.event.addListener(marker, 'click', () => {
                        SimpleLightBox.open({
                            items: allScreenshots,
                            startAt: allScreenshots.findIndex(u => u === url)
                        });
                    });
                }
                arr.push(status);
            }

            if (data.state === 'Crashed') {
                let status = data.statuses[data.statuses.length - 1];
                new google.maps.Marker({
                    position: { lat: status.latitude, lng: status.longitude },
                    icon: {
                        path: google.maps.SymbolPath.CIRCLE,
                        scale: 6
                    },
                    title: 'Crashed',
                    map: map
                });
            }

            if (path !== null && arr.length > 0) {
                path.setPath(arr.map(s => ({ lat: s.latitude, lng: s.longitude })))
            }
        }
    }

    static formatDuration(seconds: number) {
        let mins = Math.floor(seconds / 60);
        seconds -= mins * 60;
        seconds = Math.round(seconds);
        let hours = Math.floor(mins / 60);
        mins -= hours * 60;

        if (hours === 0 && mins < 10) {
            return mins + "m " + seconds + "s";
        } else {
            return hours + "h " + mins + "m";
        }
    }

    static secondToTime(seconds: number) {
        let mins = Math.floor(seconds / 60);
        seconds -= mins * 60;
        seconds = Math.round(seconds);
        let hours = Math.floor(mins / 60);
        mins -= hours * 60;

        return hours + ":" + Flight.pad(mins);
    }

    static pad(num: number) {
        return ('0' + num).slice(-2);
    }
}

export const statusToColor = (status: FlightStatus) => {
    if (status.isOnGround) return '#0000FF';
    if (status.isAutopilotOn) return '#00FF00';
    return '#FF00FF';
}

export const statusToThickness = (status: FlightStatus) => {
    if (status.isOnGround) return 2;
    return 5;
}

export default GooglePageWrapper(Flight);