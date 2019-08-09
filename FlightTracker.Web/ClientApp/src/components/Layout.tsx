import React, { Component } from 'react';
import { Container } from 'reactstrap';
import { NavMenu } from './NavMenu';

export class Layout extends Component {
    static displayName = Layout.name;

    render() {
        return (
            <>
                <NavMenu />
                <Container fluid style={{ height: 'calc(100% - 88px)' }}>
                    {this.props.children}
                </Container>
            </>
        );
    }
}
