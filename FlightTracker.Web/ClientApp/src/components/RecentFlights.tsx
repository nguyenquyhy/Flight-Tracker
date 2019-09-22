import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import { FlightData } from '../services/Models';
import FlightsTable from './FlightsTable';
import { ServicesContext } from '../Context';

interface State {
    flights?: FlightData[];
}

export default class RecentFlight extends Component<any, State> {
    static displayName = RecentFlight.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props: any) {
        super(props);
        this.state = {}
    }

    async componentDidMount() {
        const flights = await this.context.api.getFlights(3);
        this.setState({
            flights: flights
        });
    }

    handleDeleted(id: string) {
        if (this.state.flights) {
            this.setState({ flights: this.state.flights.filter(f => f.id !== id) });
        }
    }

    public render() {
        if (!this.state.flights) return null;
        return <>
            <FlightsTable header='Recent Flights' flights={this.state.flights} onDeleted={id => this.handleDeleted(id)} />
            <Link to='/flights'>View more</Link>
        </>
    }
}