import React, { Component } from "react";
import { RouteComponentProps } from "react-router";
import SimpleLightBox from "simple-lightbox";
import "simple-lightbox/dist/simpleLightbox.css";
import GooglePageWrapper from "./hoc/GooglePageWrapper";
import { FlightData, FlightStatus } from "../services/Models";
import { ServicesContext, ConfigsContext } from "../Context";

interface State {
    loading: boolean;
    deleting: boolean;
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
        this.state = { flight: null, loading: true, deleting: false };
    }

    componentDidMount() {
        this.populateData();
    }

    onDelete() {
        if (window.confirm('Are you sure to delete the flight?')) {
            this.setState({ deleting: true }, async () => {
                await this.context.api.deleteFlight(this.props.match.params.id);
                this.props.history.goBack();
            })
        }
    }

    async handleSaveTitle(value: string) {
        const flight = await this.context.api.patchFlight(this.props.match.params.id, {
            title: value
        });

        this.setState({ flight: flight });
    }

    async handleSaveDescription(value: string) {
        const flight = await this.context.api.patchFlight(this.props.match.params.id, {
            description: value
        });

        this.setState({ flight: flight });
    }

    public render() {
        if (this.state.loading) return <p><em>Loading...</em></p>;

        return <ConfigsContext.Consumer>
            {context => {
                const flight = this.state.flight;
                if (!flight) return <p><strong>Cannot load flight</strong></p>;

                const canEdit = context.configs ? context.configs.permissions["Flight"].edit : false;
                const canDelete = context.configs ? context.configs.permissions["Flight"].delete : false;
                const startDateTime = new Date(flight.startDateTime);
                const duration = flight.state === 'Enroute' ?
                    `(${Flight.formatDuration((new Date().getTime() - startDateTime.getTime()) / 1000)})` : '';

                return <>
                    <div style={{ float: 'right' }}>
                        {canDelete && <button className="btn btn-link" onClick={() => this.onDelete()} disabled={this.state.deleting}>{this.state.deleting ? "Deleting..." : "Delete"}</button>}
                    </div>
                    <TextInput type={TextInputType.Title} title='Title' disabled={!canEdit} value={flight.title || ''} onSave={value => this.handleSaveTitle(value)} />

                    <TextInput type={TextInputType.TextArea} title='Description' disabled={!canEdit} value={flight.description || ''} onSave={value => this.handleSaveDescription(value)} />

                    <table className="table table-strip">
                        <thead>
                            <tr>
                                <th>Information</th><th>Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr><td>Airports</td><td>{flight.airportFrom || '?'} - {flight.airportTo || '?'}</td></tr>
                            {flight.aircraft && <tr><td>Aircraft</td><td>{flight.aircraft.title}</td></tr>}
                            <tr><td>Flight Number</td><td>{flight.airline} {flight.flightNumber}</td></tr>
                            <tr><td>State</td><td>{flight.state}</td></tr>
                            <tr><td>Local Time</td><td>{startDateTime.toLocaleDateString()} {startDateTime.toLocaleTimeString()} {duration}</td></tr>
                            {flight.statusTakeOff && flight.statusLanding ?
                                <>
                                    <tr><td>Take-off/Landing Time</td><td>{Flight.secondToTime(flight.takeOffLocalTime)} - {Flight.secondToTime(flight.takeOffLocalTime + flight.statusLanding.simTime - flight.statusTakeOff.simTime)}</td></tr>
                                    <tr><td>Full Duration</td><td>{Flight.formatDuration(flight.statusLanding.simTime - flight.statusTakeOff.simTime)}</td></tr>
                                    <tr><td>Fuel Used</td><td>{Math.round((flight.statusTakeOff.fuelTotalQuantity - flight.statusLanding.fuelTotalQuantity) * 10) / 10} lb</td></tr>
                                </> :
                                <>
                                    <tr><td>Take-off Time</td><td>{Flight.secondToTime(flight.takeOffLocalTime)}</td></tr>
                                </>
                            }
                            {flight.statusTakeOff && <tr><td>Take-off IAS</td><td>{flight.statusTakeOff.indicatedAirSpeed}</td></tr>}
                            {flight.statusLanding &&
                                <>
                                    <tr><td>Landing IAS</td><td>{flight.statusLanding.indicatedAirSpeed}</td></tr>
                                    <tr><td>Landing VS</td><td>{flight.statusLanding.verticalSpeed}</td></tr>
                                </>
                            }
                        </tbody>
                    </table>
                    <p><em>*Blue: on the ground. Purple: autopilot off. Green: autopilot on</em></p>
                    <div id="map" style={{ width: '100%', height: 800 }}></div>
                </>
            }}
        </ConfigsContext.Consumer>
    }

    async populateData() {
        const data = await this.context.api.getFlight(this.props.match.params.id);
        this.setState({ flight: data, loading: false });

        const statuses = await this.context.api.getFlightRoute(data.id);

        if (statuses && statuses.length > 0) {
            let mapElement = document.getElementById('map') as HTMLDivElement;

            let north = statuses.reduce((prev, curr) => Math.max(prev, curr.latitude), -1000);
            let south = statuses.reduce((prev, curr) => Math.min(prev, curr.latitude), 1000);
            let east = statuses.reduce((prev, curr) => Math.max(prev, curr.longitude), -1000);
            let west = statuses.reduce((prev, curr) => Math.min(prev, curr.longitude), 1000);

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

            let allScreenshots = statuses.filter(o => o.screenshotUrl).map(o => o.screenshotUrl);

            for (let status of statuses) {
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
                let status = statuses[statuses.length - 1];
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

        if (hours === 0) {
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


interface TextInputState {
    hovering: boolean;
    editing: boolean;

    value: string;
}

interface TextInputProps {
    title: string;
    value: string;
    onSave: (value: string) => void;
    type: TextInputType;
    disabled?: boolean;
}

enum TextInputType {
    Title,
    TextArea
}

class TextInput extends Component<TextInputProps, TextInputState> {
    constructor(props: TextInputProps) {
        super(props);

        this.state = { hovering: false, editing: false, value: props.value }
    }

    handleCancel() {
        this.setState({ editing: false, value: this.props.value })
    }

    handleSave() {
        this.props.onSave(this.state.value)
        this.setState({ editing: false })
    }

    public render() {
        switch (this.props.type) {
            case TextInputType.Title:
                if (this.props.disabled)
                    return <h2>{this.props.value}</h2>;
                if (!this.state.editing) {
                    if (!this.state.value)
                        return <h2 onClick={() => this.setState({ editing: true })}>Click to enter {this.props.title}</h2>;
                    return <h2 onClick={() => this.setState({ editing: true })}>{this.props.value}</h2>;
                }
                return <div className="input-group" style={{ marginBottom: 8, width: 'calc(100% - 100px)' }}>
                    <div className="input-group-prepend">
                        <button className="btn btn-sm btn-primary" onClick={e => this.handleSave()}>Save</button>
                        <button className="btn btn-sm btn-warning" onClick={e => this.handleCancel()}>Cancel</button>
                    </div>
                    <input className="form-control" value={this.state.value} onChange={e => this.setState({ value: e.target.value, editing: true })} />
                </div>
            case TextInputType.TextArea:
                if (this.props.disabled)
                    return <p>{this.props.value}</p>;
                if (!this.state.editing) {
                    if (!this.state.value)
                        return <p onClick={() => this.setState({ editing: true })}><em>Click to enter {this.props.title}</em></p>;
                    return <p onClick={() => this.setState({ editing: true })}>{this.props.value}</p>;
                }
                return <div style={{ marginBottom: 10 }}>
                    <textarea value={this.state.value} placeholder={'Enter ' + this.props.title} className="form-control" onChange={e => this.setState({ value: e.target.value, editing: true })} />
                    <div>
                        <button className="btn btn-sm btn-primary" onClick={e => this.handleSave()}>Save</button>
                        <button className="btn btn-sm btn-warning" onClick={e => this.handleCancel()}>Cancel</button>
                    </div>
                </div>
        }
    }
}