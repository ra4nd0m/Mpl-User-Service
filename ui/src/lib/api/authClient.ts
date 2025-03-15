import { authStore } from "$lib/stores/auth"

export async function fetchWithAuth(url: string, options: RequestInit = {}) {
    let token = '';
    authStore.subscribe(state => {
        token = state.token || '';
    })();
    options.headers = {
        ...options.headers,
        Authorization: `Bearer ${token}`
    };
    let response = await fetch(url, options);

    if (response.status === 401 && response.headers.get('Token-Expired') === 'true') {
        const newToken = await refreshAccessToken();
        if (newToken) {
            authStore.setToken(newToken);
            options.headers = {
                ...options.headers,
                Authorization: `Bearer ${newToken}`
            };
            response = await fetch(url, options);
        };
    }
    return response;
};

async function refreshAccessToken(): Promise<string | null> {
    const response = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' });

    if (response.ok) {
        const data = await response.json();
        return data.token;
    }
    return null;
}