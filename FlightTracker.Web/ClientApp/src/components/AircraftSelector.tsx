import React, { Component } from "react";
import styled, { css } from "styled-components";
import { Modal, ModalBody, ModalHeader } from "reactstrap";
import { ServicesContext } from "../Context";
import { AircraftData } from "../services/Models";

interface State {
    aircrafts?: { aircraft: AircraftData, selected: boolean }[];
}

interface Props {
    isOpen: boolean;
    selected: AircraftData;
    onSelected: (aircraft: AircraftData) => void;
}

export default class AircraftSelector extends Component<Props, State> {
    static displayName = AircraftSelector.name;
    static contextType = ServicesContext;

    context!: React.ContextType<typeof ServicesContext>;

    constructor(props: Props) {
        super(props);

        this.state = {

        }
    }

    async componentDidMount() {
        const aircrafts = await this.context.api.getAircraftSelections();
        this.setState({
            aircrafts: aircrafts.map(aircraft => ({
                aircraft: aircraft,
                selected: aircraft.tailNumber == this.props.selected.tailNumber
            }))
        })
    }

    public render() {
        return <Modal isOpen={this.props.isOpen}>
            <ModalHeader>
                Choose an aircraft
            </ModalHeader>
            <ModalBody>
                <StyledList>
                    {this.state.aircrafts && this.state.aircrafts.map(aircraft => (
                        <StyledItem key={aircraft.aircraft.tailNumber} selected={aircraft.selected}
                            onClick={() => this.props.onSelected(aircraft.aircraft)}>
                            {aircraft.aircraft.title}
                        </StyledItem>
                    ))}
                </StyledList>
            </ModalBody>
        </Modal>
    }
}

const StyledList = styled.ul`
padding: 0;
`

const StyledItem = styled.li`
list-style: none;
${props => props.selected && css`font-weight: bold;`}
`