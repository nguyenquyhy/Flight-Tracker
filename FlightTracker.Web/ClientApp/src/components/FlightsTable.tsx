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
                                <th>Number</th>
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
            <td><Link to={`flights/${flight.id}`}>{flight.flightNumber}</Link></td>
            <td><Link to={`flights/${flight.id}`}>{!!flight.hasScreenshots && screenshotIcon}{!!flight.videoUrl && videoIcon}{flight.title || 'Unnamed'}</Link></td>
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

const StyledIcon = styled.svg`
margin-right: 8px;
margin-top: -2px;
`

const Red = styled.span`color: red`

const StyledRow = styled.tr`
span.state {
${props =>
        props.state === 'CRASHED' ? css`color: red;` :
            props.state === 'ENROUTE' ? css`color: white;` : ''}
}
`

const videoIcon = <StyledIcon xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" stroke="#d6c86b"><path d="M22 1v2h-2v-2h-2v4h-12v-4h-2v2h-2v-2h-2v22h2v-2h2v2h2v-4h12v4h2v-2h2v2h2v-22h-2zm-18 18h-2v-2h2v2zm0-4h-2v-2h2v2zm0-4h-2v-2h2v2zm0-4h-2v-2h2v2zm14 9h-12v-8h12v8zm4 3h-2v-2h2v2zm0-4h-2v-2h2v2zm0-4h-2v-2h2v2zm0-4h-2v-2h2v2z" /></StyledIcon>;
const screenshotIcon = <StyledIcon xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 512 512" fill="#d6c86b"><g>
	<path d="M60,150.5c0,11.046,8.954,20,20,20s20-8.954,20-20v-50h49c11.046,0,20-8.954,20-20s-8.954-20-20-20h-49v-40
		c0-11.046-8.954-20-20-20s-20,8.954-20,20v40H20c-11.046,0-20,8.954-20,20s8.954,20,20,20h40V150.5z"/>
	<path d="M432,210.5c-11.046,0-20,8.954-20,20v52c0,11.046,8.954,20,20,20c11.046,0,20-8.954,20-20v-52
		C452,219.454,443.046,210.5,432,210.5z"/>
	<path d="M362,100.5h10c22.056,0,40,17.944,40,40v10c0,11.046,8.954,20,20,20c11.046,0,20-8.954,20-20v-10
		c0-44.112-35.888-80-80-80h-10c-11.046,0-20,8.954-20,20S350.954,100.5,362,100.5z"/>
	<path d="M80,210.5c-11.046,0-20,8.954-20,20v52c0,11.046,8.954,20,20,20s20-8.954,20-20v-52C100,219.454,91.046,210.5,80,210.5z"
		/>
	<path d="M150,411.5h-10c-22.056,0-40-17.944-40-40v-9c0-11.046-8.954-20-20-20s-20,8.954-20,20v9c0,44.112,35.888,80,80,80h10
		c11.046,0,20-8.954,20-20C170,420.454,161.046,411.5,150,411.5z"/>
	<path d="M492,411.5h-40v-49c0-11.046-8.954-20-20-20c-11.046,0-20,8.954-20,20v49h-50c-11.046,0-20,8.954-20,20
		c0,11.046,8.954,20,20,20h50v40c0,11.046,8.954,20,20,20c11.046,0,20-8.954,20-20v-40h40c11.046,0,20-8.954,20-20
		C512,420.454,503.046,411.5,492,411.5z"/>
	<path d="M282,60.5h-52c-11.046,0-20,8.954-20,20s8.954,20,20,20h52c11.046,0,20-8.954,20-20S293.046,60.5,282,60.5z"/>
	<path d="M282,411.5h-52c-11.046,0-20,8.954-20,20c0,11.046,8.954,20,20,20h52c11.046,0,20-8.954,20-20
		C302,420.454,293.046,411.5,282,411.5z"/>
</g></StyledIcon>;