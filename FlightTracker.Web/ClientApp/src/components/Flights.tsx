import React, { Component } from 'react';
import { FlightData } from '../services/Models';
import FlightsTable from './FlightsTable';
import { ServicesContext } from '../Context';

interface State {
    loading: boolean;
    flights: FlightData[]
}

export class Flights extends Component<any, State> {
    static displayName = Flights.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props) {
        super(props);
        this.state = { flights: [], loading: true };
    }

    componentDidMount() {
        this.populateData();
    }

    handleDeleted(id: string) {
        if (this.state.flights) {
            this.setState({ flights: this.state.flights.filter(f => f.id !== id) });
        }
    }

    renderFlightsTable(flights: FlightData[]) {
        return (
            <FlightsTable flights={flights} onDeleted={id => this.handleDeleted(id)} />
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderFlightsTable(this.state.flights);

        return (
            <div>
                <h1>Flights</h1>
                {contents}
            </div>
        );
    }

    async populateData() {
        const flights = await this.context.api.getFlights();
        this.setState({ flights: flights, loading: false });
    }
}
