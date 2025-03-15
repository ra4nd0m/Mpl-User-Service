import { writable } from "svelte/store";

interface User {
    id: string;
    email: string;
    subscriptionType?: string;
    subscriptionEnd?: string;
}

interface AuthState {
    isAuthenticated: boolean;
    user: User | null;
    loading: boolean;
    token: string | null;
}

function parseJwt(token: string) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error('Error parsing JWT', error);
        return null;
    }
}

const createAuthStore = () => {
    const initialState: AuthState = {
        isAuthenticated: false,
        user: null,
        loading: false,
        token: null
    };

    const { subscribe, update } = writable<AuthState>(initialState);

    return {
        subscribe,
        setToken: (token: string) => {
            const claims = parseJwt(token);
            if (!claims) return;
            const user: User = {
                id: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
                email: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
                subscriptionType: claims['SubscriptionType'],
                subscriptionEnd: claims['SubscriptionEnd']
            };

            update(state => ({
                ...state,
                isAuthenticated: true,
                user,
                token
            }));
        },
        clearAuth: () => {
            update(state => ({
                ...state,
                isAuthenticated: false,
                user: null,
                token: null
            }));
        },
        setLoading: (loading: boolean) => {
            update(state => ({
                ...state,
                loading
            }));
        }
    };
};

export const authStore = createAuthStore();