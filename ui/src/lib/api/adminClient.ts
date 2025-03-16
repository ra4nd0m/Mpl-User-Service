import { fetchWithAuth } from "./authClient";

export async function getOrgs(): Promise<OrgResponse[] | null> {
    try {
        const response = await fetchWithAuth('organizations');
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
        const response = await fetchWithAuth(`organizations/${id}`);
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

export interface OrgResponse {
    name: string;
    inn: string;
    subscriptionType: SubscriptionType;
    subscriptionStartDate: string;
    subscriptionEndDate: string;
}

export async function registerUser(user: NewUser): Promise<RegisterUserResponse | null> {
    try {
        const response = await fetchWithAuth('register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(user)
        });

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

export enum SubscriptionType {
    Free = "Free",
    Basic = "Basic",
    Premium = "Premium"
}

export interface RegisterUserResponse {
    id: string;
    email: string;
    organizationId: number;
}

export interface NewUser {
    email: string;
    password: string;
    organization: {
        name: string;
        inn: string;
        subscriptionType: SubscriptionType;
        subscriptionStartDate: string;
        subscriptionEndDate: string;
    }
}