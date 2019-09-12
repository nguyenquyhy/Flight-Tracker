import React, { Component } from 'react'
import { ServicesContext } from '../Context';
import { AircraftData } from '../services/Models';

interface State {
    loading: boolean;
    aircrafts: AircraftData[];
}

export default class Aircrafts extends Component<any, State> {
    static displayName = Aircrafts.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props: any) {
        super(props);

        this.state = { aircrafts: [], loading: true }
    }

    async componentDidMount() {
        const aircrafts = await this.context.api.getAircrafts();
        this.setState({ loading: false, aircrafts: aircrafts })
    }

    public render() {
        return <>
            <h1>Aircrafts</h1>

            <table className='table table-striped'>
                <thead>
                    <tr>
                        <th>Tail Number</th>
                        <th>Title</th>
                        <th>Model</th>
                        <th>Type</th>
                    </tr>
                </thead>
                <tbody>
                    {this.state.aircrafts.map(aircraft => (
                        <tr>
                            <td>{aircraft.tailNumber}</td>
                            <td>{aircraft.title}</td>
                            <td>{aircraft.model}</td>
                            <td>{aircraft.type}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </>
    }
}