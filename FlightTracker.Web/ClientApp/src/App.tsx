import { hot } from 'react-hot-loader/root';
import React, { Component } from 'react';
import { Route } from 'react-router';
import ApiService from './services/ApiService';
import { PrivateRoute } from './PrivateRoute';
import { Layout } from './components/Layout';
import Home from './components/Home';
import { Flights } from './components/Flights';
import Flight from './components/Flight';
import Aircrafts from './components/Aircrafts';
import { Admin } from './components/Admin';

import './custom.css'

import { ServicesContext, ConfigsContext } from './Context';
import { Configs } from './services/Models';

interface State {
    configs?: Configs;
}

class App extends Component<any, State> {
    static displayName = App.name;

    api: ApiService = new ApiService();

    constructor(props: any) {
        super(props);

        this.state = {}
    }

    async componentDidMount() {
        const configs = await this.api.getConfigs();
        this.setState({ configs: configs });
    }

    render() {
        return (
            <ServicesContext.Provider value={{ api: this.api }}>
                <ConfigsContext.Provider value={{ configs: this.state.configs }}>
                    <Layout>
                        <Route exact path='/' component={Home} />
                        <Route path='/flights' exact component={Flights} />
                        <Route path='/flights/:id' component={Flight} />
                        <Route path='/aircrafts' exact component={Aircrafts} />
                        <PrivateRoute path='/admin' component={Admin} />
                    </Layout>
                </ConfigsContext.Provider>
            </ServicesContext.Provider>
        );
    }
}

export default hot(App);