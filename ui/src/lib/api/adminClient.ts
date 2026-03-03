import { fetchWithAuth } from "./authClient";

export async function getOrgs(): Promise<OrgResponse[] | null> {
    try {
        const response = await fetchWithAuth('organizations', {}, true);
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to get orgs:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during getOrgs:', error);
        return null;
    }
}

export async function getOrg(id: number): Promise<OrgResponse | null> {
    try {
        const response = await fetchWithAuth(`organizations/${id}`, {}, true);
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to get org:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (err) {
        console.error('Error during getOrg:', err);
        return null;
    }

}

export async function createOrg(org: OrgCreateRequest): Promise<OrgResponse | null> {
    try {
        const response = await fetchWithAuth('organizations', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(org)
        }, true);

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to create org:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during createOrg:', error);
        return null;
    }
}

export async function updateOrg(id: number, updatedData: OrgCreateRequest): Promise<OrgResponse | null> {
    try {
        const response = await fetchWithAuth(`organizations/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedData)
        }, true);

        if (!response.ok) {
            console.error('Failed to update org:', await response.json().catch(() => response.statusText));
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during updateOrg:', error);
        return null;
    }
}

export async function registerUser(user: NewUser): Promise<UserResponse | null> {
    try {
        const response = await fetchWithAuth('register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(user)
        }, true);

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to register user:', errorData || response.statusText);
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error('Error during user registration:', error);
        return null;
    }
}

export async function updateUser(email: string, updatedData: UpdatedUser): Promise<UserResponse | null> {
    try {
        const response = await fetchWithAuth(`users/${email}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedData)
        }, true);

        if (!response.ok) {
            console.error('Failed to update user:', await response.json().catch(() => response.statusText));
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during updateUser:', error);
        return null;
    }
}

export async function getUserByEmail(email: string): Promise<UserResponse | null> {
    try {
        const response = await fetchWithAuth(`users/${email}`, {}, true);
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to get user:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during getUserByEmail:', error);
        return null;
    }
}

export async function getUsers(): Promise<UserResponse[] | null> {
    try {
        const response = await fetchWithAuth('users', {}, true);
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to get users:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during getUsers:', error);
        return null;
    }
}

export async function deleteUser(email: string): Promise<boolean> {
    try {
        const response = await fetchWithAuth(`users/${email}`, { method: 'DELETE' }, true);

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to delete user:', errorData || response.statusText);
            return false;
        }

        return true;
    } catch (err) {
        console.error('Error during deleteUser:', err);
        return false;
    }
}

export async function getFilters(): Promise<DataFilter[] | null> {
    try {
        const response = await fetchWithAuth('data/filter-config/filters');
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to get filters:', errorData || response.statusText);
            return null;
        }
        return await response.json();
    } catch (error) {
        console.error('Error during getFilters:', error);
        return null;
    }
}

export async function pushFilter(filter: DataFilter): Promise<void> {
    try {
        const response = await fetchWithAuth('data/filter-config/filter', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(filter)
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Failed to push filter:', errorData || response.statusText);
        }
    } catch (error) {
        console.error('Error during pushFilter:', error);
    }
}

export enum SubscriptionType {
    Free = 0,
    Basic = 1,
    Premium = 2
}

export interface UserResponse {
    id: string;
    email: string;
    org?: OrgResponse | null;
    sub?: SubscriptionDataDto | null;
    canExportData?: boolean;
}

export interface OrgCreateRequest {
    name: string;
    inn: string;
    subscriptionType: SubscriptionType;
    subscriptionStartDate: string; // ISO date string format
    subscriptionEndDate: string;   // ISO date string format
}

export interface OrgResponse {
    id: number;
    name: string;
    inn: string;
    subscriptionType: SubscriptionType;
    subscriptionStartDate: string; // ISO date string format
    subscriptionEndDate: string;   // ISO date string format
}

export interface NewUser {
    email: string;
    password: string;
    organization?: OrgResponse | null;
    sub?: SubscriptionDataDto | null;
    canExportData?: boolean;
}

export interface UpdatedUser {
    newEmail?: string;
    password?: string;
    organization?: OrgResponse | null;
    sub?: SubscriptionDataDto | null;
    canExportData?: boolean;
}

export interface DataFilter {
    affectedRole: string;
    groups: number[];
    sources: number[];
    units: number[];
    materialIds: number[];
    properties: number[];
}

export interface SubscriptionDataDto {
    subscriptionType: SubscriptionType;
    subscriptionStartDate: string;
    subscriptionEndDate: string;
}