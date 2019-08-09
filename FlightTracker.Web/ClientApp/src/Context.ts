import React from 'react';
import ApiService from './services/ApiService';

interface ServicesContext {
    api: ApiService;
}

export const ServicesContext = React.createContext<ServicesContext>({ api: new ApiService() });

interface GoogleMapsContext {
    key?: string;
}

export const GoogleMapsContext = React.createContext<GoogleMapsContext>({})