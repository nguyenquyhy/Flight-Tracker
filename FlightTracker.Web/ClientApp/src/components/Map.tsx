import React, { Component } from 'react';
import { GoogleMapsContext } from '../Context';

export default class Map extends Component {
    static displayName = Map.name;
    static contextType = GoogleMapsContext;

    public render() {
        return <GoogleMapsContext.Consumer>
            {context => context.key}
        </GoogleMapsContext.Consumer>
    }
}