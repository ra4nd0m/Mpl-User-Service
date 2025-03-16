interface Config {
    apiBaseUrl: string;
    apiAuthUrl: string;
}

const config: Config = {
    apiBaseUrl: import.meta.env.PUBLIC_API_BASE_URL,
    apiAuthUrl: import.meta.env.PUBLIC_API_AUTH_URL
};

if (!config.apiBaseUrl) {
    if (import.meta.env.DEV) {
        config.apiBaseUrl = 'http://localhost:5000/userapi';
    } else {
        throw new Error('Missing PUBLIC_API_BASE_URL');
    }
}

if (!config.apiAuthUrl) {
    if (import.meta.env.DEV) {
        config.apiAuthUrl = 'http://localhost:5000/authapi';
    } else {
        throw new Error('Missing PUBLIC_API_AUTH_URL');
    }
}

export default config;