import { writable } from "svelte/store";

export interface User {
    id: string;
    email: string;
    subscriptionType?: string;
    subscriptionEnd?: string;
    canExportData: boolean;
}

interface AuthState {
    isAuthenticated: boolean;
    user: User | null;
    loading: boolean;
    token: string | null;
    roles: string[];
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
        token: null,
        roles: []
    };

    const { subscribe, update } = writable<AuthState>(initialState);

    return {
        subscribe,
        setToken: (token: string) => {
            const claims = parseJwt(token);
            if (!claims) return;

            let roles: string[] = [];
            const roleClaim = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            if (roleClaim) {
                if (Array.isArray(roleClaim)) {
                    roles = roleClaim;
                } else {
                    roles = [roleClaim];
                }
            }

            let subscriptionType = claims['SubscriptionType'];

            const isAdmin = roles.includes('Admin');
            if (!subscriptionType && isAdmin) {
                subscriptionType = 'Admin';
            }

            const canExportData = claims['CanExportData'] === 'True';

            const user: User = {
                id: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
                email: claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
                subscriptionType: subscriptionType,
                subscriptionEnd: claims['SubscriptionEnd'],
                canExportData
            };

            update(state => ({
                ...state,
                isAuthenticated: true,
                user,
                token,
                roles
            }));
        },
        clearAuth: () => {
            update(state => ({
                ...state,
                isAuthenticated: false,
                user: null,
                token: null,
                roles: []
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