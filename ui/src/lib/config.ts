interface Config {
    apiBaseUrl: string;
    apiAuthUrl: string;
}

const config: Config = {
    apiBaseUrl: import.meta.env.VITE_API_BASE_URL,
    apiAuthUrl: import.meta.env.VITE_API_AUTH_URL
};

if (!config.apiBaseUrl) {
    if (import.meta.env.DEV) {
        config.apiBaseUrl = import.meta.env.PUBLIC_DEV_API_BASE_URL;
    } else {
        throw new Error('Missing PUBLIC_API_BASE_URL');
    }
}

if (!config.apiAuthUrl) {
    if (import.meta.env.DEV) {
        config.apiAuthUrl = import.meta.env.PUBLIC_DEV_API_AUTH_URL;
    } else {
        throw new Error('Missing PUBLIC_API_AUTH_URL');
    }
}

export default config;