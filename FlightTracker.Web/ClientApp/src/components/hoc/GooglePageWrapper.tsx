import React from "react";
import { withRouter } from "react-router";
import GoogleApiWrapper from "./GoogleApiWrapper";
import { ConfigsContext } from "../../Context";

export default (component) => (props) => {
    return <>
        <ConfigsContext.Consumer>
            {context => {
                if (!context.configs || !context.configs.googleMapsKey) return null;

                const WrapperComponent = withRouter(GoogleApiWrapper({ apiKey: context.configs.googleMapsKey })(component))
                return <WrapperComponent />
            }}
        </ConfigsContext.Consumer>
    </>
}