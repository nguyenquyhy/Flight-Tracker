import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import styled, { css } from 'styled-components';
import { FlightData } from '../services/Models';
import { ConfigsContext, ServicesContext } from '../Context';
import { secondsToTime } from './Flight';

interface Props {
    header?: string;
    flights: FlightData[];
    onDeleted: (id: string) => void;
}

export default class FlightsTable extends Component<Props> {
    static displayName = FlightsTable.name;

    public render() {
        return <ConfigsContext.Consumer>
            {context => (
                <>
                    <StyledHeader>{this.props.header}</StyledHeader>
                    <StyledTable className='table table-borderless'>
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Time</th>
                                <th>Flight</th>
                                <th>Depart</th>
                                <th>Arrive</th>
                                <th>Aircraft</th>
                                <th>Remarks</th>
                                {context.configs && context.configs.permissions["Flight"].delete && <th>&nbsp;</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {this.props.flights.map(flight =>
                                <Row key={flight.id} flight={flight} onDeleted={() => this.props.onDeleted(flight.id)}
                                    canDelete={context.configs ? context.configs.permissions["Flight"].delete : false} />
                            )}
                        </tbody>
                    </StyledTable>
                </>
            )}
        </ConfigsContext.Consumer>
    }
}

const StyledHeader = styled.div`
font-size: 1.5em;
background: #d6c86b;
border-left: 15px solid #7a8eae;
border-right: 15px solid #7a8eae;
margin-left: -15px;
margin-right: -15px;
font-weight: bold;
padding: 20px 10px 0 10px;
text-transform: uppercase;
`

const StyledTable = styled.table`
text-transform: uppercase;

border-left: 15px solid #7a8eae;
border-right: 15px solid #7a8eae;
//border-bottom: 8px solid #7a8eae;
margin-left: -15px;
margin-right: -15px;
box-sizing: content-box;
width: calc(100% + 30px);
box-shadow: 3px 3px 3px #888888;

thead {
background: #d6c86b;
}

tbody {
background: #2e3545;
font-weight: bold;

td {
border-top: 6px solid #000;
}
}

a {
color: #d6c86b;
}

button {
margin-top: -3px;
margin-bottom: -3px;
}
`

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

    private renderTime(flight: FlightData) {
        const takeOffTime = !!flight.statusTakeOff ? secondsToTime(flight.statusTakeOff.localTime) : '';
        switch (flight.state) {
            case "STARTED": return <>-</>;
            case "ENROUTE": return <>{takeOffTime} - </>
            case "CRASHED": return <>{takeOffTime} - <Red>{flight.route && secondsToTime(flight.route[0].localTime)}</Red></>
            case "ARRIVED": return <>{takeOffTime} - {!!flight.statusLanding && secondsToTime(flight.statusLanding.localTime)}</>
        }
    }

    public render() {
        const flight = this.props.flight;
        return <StyledRow state={flight.state}>
            <td><Link to={`flights/${flight.id}`}>{new Date(flight.startDateTime).toLocaleDateString()}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{this.renderTime(flight)}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.title || 'Unnamed'}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.airportFrom}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.airportTo}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{flight.aircraft ? flight.aircraft.title : ''}</Link></td>
            <td><Link to={`flights/${flight.id}`}><span className='state'>{flight.state}</span></Link></td>
            {this.props.canDelete && <td>
                <button className='btn btn-danger btn-sm' onClick={() => this.onDelete()} disabled={this.state.deleting}>{this.state.deleting ? "Deleting..." : "Delete"}</button>
            </td>}
        </StyledRow>
    }
}

const Red = styled.span`color: red`

const StyledRow = styled.tr`
span.state {
${props =>
        props.state === 'CRASHED' ? css`color: red;` :
            props.state === 'ENROUTE' ? css`color: white;` : ''}
}
`