import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import { FlightData } from '../services/Models';
import { ConfigsContext, ServicesContext } from '../Context';

interface Props {
    flights: FlightData[];
    onDeleted: (id: string) => void;
}

export default class FlightsTable extends Component<Props> {
    static displayName = FlightsTable.name;

    public render() {
        return <ConfigsContext.Consumer>
            {context => (
                <table className='table table-striped'>
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Title</th>
                            <th>From</th>
                            <th>To</th>
                            <th>Aircraft</th>
                            <th>State</th>
                            {context.configs && context.configs.permissions["Flight"].delete && <th>&nbsp;</th>}
                        </tr>
                    </thead>
                    <tbody>
                        {this.props.flights.map(flight =>
                            <Row key={flight.id} flight={flight} onDeleted={() => this.props.onDeleted(flight.id)}
                                canDelete={context.configs ? context.configs.permissions["Flight"].delete : false} />
                        )}
                    </tbody>
                </table>
            )}
        </ConfigsContext.Consumer>
    }


    static stateToRowClassName(state: string) {
        switch (state) {
            case 'Crashed': return 'table-danger';
            case 'Enroute': return 'table-primary';
        }
        return '';
    }
}

interface RowState {
    deleting: boolean;
}

interface RowProps {
    flight: FlightData;
    onDeleted: () => void;

    canDelete: boolean;
}

class Row extends Component<RowProps, RowState> {
    static displayName = Row.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props: RowProps) {
        super(props);

        this.state = { deleting: false }
    }

    async onDelete() {
        if (window.confirm('Are you sure to delete the flight?')) {
            this.setState({ deleting: true }, async () => {
                await this.context.api.deleteFlight(this.props.flight.id);
                this.props.onDeleted();
                this.setState({ deleting: false });
            })
        }
    }

    public render() {
        const flight = this.props.flight;
        return <tr className={FlightsTable.stateToRowClassName(flight.state)}>
            <td><Link to={`flights/${flight.id}`}>{new Date(flight.startDateTime).toLocaleDateString()}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.title || 'Unnamed'}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.airportFrom}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.airportTo}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.aircraft ? flight.aircraft.title : ''}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.state}</Link></td>
            {this.props.canDelete && <td>
                <button className='btn btn-danger' onClick={() => this.onDelete()} disabled={this.state.deleting}>{this.state.deleting ? "Deleting..." : "Delete"}</button>
            </td>}
        </tr>
    }
}