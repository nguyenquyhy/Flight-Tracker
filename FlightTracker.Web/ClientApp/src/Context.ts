import React from 'react';
import ApiService from './services/ApiService';
import { Configs } from './services/Models';

interface ServicesContext {
    api: ApiService;
}

export const ServicesContext = React.createContext<ServicesContext>({ api: new ApiService() });

interface ConfigsContext {
    configs?: Configs;
}

export const ConfigsContext = React.createContext<ConfigsContext>({})