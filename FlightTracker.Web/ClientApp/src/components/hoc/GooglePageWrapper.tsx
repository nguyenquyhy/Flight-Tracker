import React from "react";
import { withRouter } from "react-router";
import GoogleApiWrapper from "./GoogleApiWrapper";
import { GoogleMapsContext } from "../../Context";

export default (component) => (props) => {
    return <>
        <GoogleMapsContext.Consumer>
            {configs => {
                if (!configs.key) return null;

                const WrapperComponent = withRouter(GoogleApiWrapper({ apiKey: configs.key })(component))
                return <WrapperComponent />
            }}
        </GoogleMapsContext.Consumer>
    </>
}