import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import { FlightData } from '../services/Models';

interface Props {
    flights: FlightData[];
}

export default class FlightsTable extends Component<Props> {
    public render() {
        return <table className='table table-striped'>
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Title</th>
                    <th>From</th>
                    <th>To</th>
                    <th>Aircraft</th>
                    <th>State</th>
                </tr>
            </thead>
            <tbody>
                {this.props.flights.map(flight =>
                    <tr key={flight.id} className={FlightsTable.stateToRowClassName(flight.state)}>
                        <td><Link to={`flights/${flight.id}`}>{new Date(flight.startDateTime).toLocaleDateString()}</Link></td>
                        <td><Link to={`flights/${flight.id}`}>{flight.title || 'Unnamed'}</Link></td>
                        <td><Link to={`flights/${flight.id}`}>{flight.airportFrom}</Link></td>
                        <td><Link to={`flights/${flight.id}`}>{flight.airportTo}</Link></td>
                        <td><Link to={`flights/${flight.id}`}>{flight.aircraft ? flight.aircraft.title : ''}</Link></td>
                        <td><Link to={`flights/${flight.id}`}>{flight.state}</Link></td>
                    </tr>
                )}
            </tbody>
        </table>
    }


    static stateToRowClassName(state: string) {
        switch (state) {
            case 'Crashed': return 'table-danger';
            case 'Enroute': return 'table-primary';
        }
        return '';
    }
}