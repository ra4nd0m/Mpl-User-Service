import { authStore } from "$lib/stores/authStore";
import config from "$lib/config";

export async function fetchWithAuth(url: string, options: RequestInit = {}, useAuthService: boolean = false) {
    let token = '';
    authStore.subscribe(state => {
        token = state.token || '';
    })();

    if (token) {
        options.headers = {
            ...options.headers,
            Authorization: `Bearer ${token}`
        };
    }

    const normalizedUrl = url.startsWith('/') ? url.substring(1) : url;
    let finalUrl: string;
    if (useAuthService) {
        finalUrl = `${config.apiAuthUrl}/${normalizedUrl}`;
    } else {
        finalUrl = `${config.apiBaseUrl}/${normalizedUrl}`;
    }
    let response = await fetch(finalUrl, options);

    if (response.status === 401 && response.headers.get('Token-Expired') === 'true') {
        const newToken = await refreshAccessToken();
        if (newToken) {
            authStore.setToken(newToken);
            options.headers = {
                ...options.headers,
                Authorization: `Bearer ${newToken}`
            };
            response = await fetch(finalUrl, options);
        };
    }
    return response;
};

export async function refreshAccessToken(): Promise<string | null> {
    try {
        const storedRememberMe = localStorage.getItem('rememberMe');
        if (!storedRememberMe) {
            await logout();
            return null;
        }
        const response = await fetch(`${config.apiAuthUrl}/refresh`, {
            method: 'POST',
            credentials: 'include'
        });

        if (response.ok) {
            const data = await response.json();
            return data.token;
        }
        return null;
    } catch (error) {
        console.error('Token refresh error:', error);
        return null;
    }
}

export async function login(email: string, password: string, rememberMe: boolean): Promise<{
    success: boolean;
    error?: string;
}> {
    try {
        const response = await fetch(`${config.apiAuthUrl}/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password }),
            credentials: 'include'
        });

        if (!response.ok) {
            const errorData = await response.json();
            return { success: false, error: errorData.message || 'Failed to login' };
        };

        const data = await response.json();
        if (data.token) {
            authStore.setToken(data.token);
            if (rememberMe) {
                // Store the token in localStorage for persistent login
                localStorage.setItem('rememberMe', JSON.stringify({ rememberMe }));
            }
            return { success: true };
        } else {
            return { success: false, error: 'No token received from server' };
        }
    } catch (error) {
        console.error('Failed to login', error);
        return { success: false, error: 'Failed to login' };
    }
}

export async function logout(): Promise<boolean> {
    try {
        localStorage.removeItem('rememberMe'); // Clear rememberMe from localStorage
        const response = await fetch(`${config.apiAuthUrl}/logout`, { method: 'POST', credentials: 'include' });
        return response.ok;
    } catch (error) {
        console.error('Failed to logout', error);
        return false;
    } finally {
        authStore.clearAuth();
    }
}