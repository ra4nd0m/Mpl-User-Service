import { authStore } from "$lib/stores/authStore";
import config from "$lib/config";

// Queue for pending requests
let pendingRequests: Array<{
    resolve: (response: Response) => void;
    reject: (error: Error) => void;
    url: string;
    options: RequestInit;
    useAuthService: boolean;
}> = [];

let isStoreReady = false;
let currentToken = '';

// Subscribe to authStore to keep track of token changes
authStore.subscribe(state => {
    currentToken = state.token || '';

    // If we now have a token and there are pending requests, process them
    if (currentToken && currentToken.length > 0) {
        if (!isStoreReady) {
            isStoreReady = true;
        }
        // Always process pending requests when we have a token
        if (pendingRequests.length > 0) {
            processPendingRequests();
        }
    } else if (!currentToken || currentToken.length === 0) {
        isStoreReady = false;
    }

});

// Function to process pending requests
async function processPendingRequests() {
    const requestsToProcess = [...pendingRequests];
    pendingRequests = []; // Clear the queue before processing
    for (const req of requestsToProcess) {
        try {
            const resp = await executeRequest(req.url, req.options, req.useAuthService);
            req.resolve(resp);
        } catch (error) {
            req.reject(error as Error);
        }
    }


}

async function executeRequest(url: string, options: RequestInit, useAuthService: boolean): Promise<Response> {
    // Add auth header
    options.headers = {
        ...options.headers,
        Authorization: `Bearer ${currentToken}`
    };

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
            currentToken = newToken;
            options.headers = {
                ...options.headers,
                Authorization: `Bearer ${newToken}`
            };
            response = await fetch(finalUrl, options);
        }
    }
    return response;
}

export async function fetchWithAuth(url: string, options: RequestInit = {}, useAuthService: boolean = false): Promise<Response> {
    return new Promise((resolve, reject) => {
        // If store is ready and we have a token, execute immediately
        if (isStoreReady && currentToken && currentToken.length > 0) {
            executeRequest(url, options, useAuthService).then(resolve).catch(reject);
        } else if (!currentToken || currentToken.length === 0) {
            // If no token and store seems ready, this might be an unauthenticated state
            if (isStoreReady) {
                reject(new Error('No valid token available'));
                return;
            }

            // Queue the request
            pendingRequests.push({
                resolve: (response: Response) => resolve(response),
                reject,
                url,
                options,
                useAuthService
            });
        } else {
            // Token exists but store might not be fully initialized, execute anyway
            executeRequest(url, options, useAuthService).then(resolve).catch(reject);
        }
    })
}


export async function refreshAccessToken(): Promise<string | null> {
    try {
        const storedRememberMe = localStorage.getItem('rememberMe') || sessionStorage.getItem('rememberMe');
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
            const errorData = await response.json().catch(() => null);
            // Try to extract the most specific error message available
            const errorMessage = errorData?.detail || errorData?.title || errorData?.message || 'Failed to login';
            return { success: false, error: errorMessage };
        };

        const data = await response.json();
        if (data.token) {
            authStore.setToken(data.token);
            if (rememberMe) {
                // Store the token in localStorage for persistent login
                localStorage.setItem('rememberMe', JSON.stringify({ rememberMe }));
            } else {
                sessionStorage.setItem('rememberMe', JSON.stringify({ rememberMe }));
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
        sessionStorage.removeItem('rememberMe'); // Clear rememberMe from sessionStorage
        const response = await fetch(`${config.apiAuthUrl}/logout`, { method: 'POST', credentials: 'include' });
        return response.ok;
    } catch (error) {
        console.error('Failed to logout', error);
        return false;
    } finally {
        authStore.clearAuth();
    }
}