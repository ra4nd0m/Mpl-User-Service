import { browser } from "$app/environment";

export const ENABLE_MOCKS = browser && (import.meta.env.DEV || import.meta.env.VITE_USE_MOCKS === 'true');

export const MOCK_DELAY = 800;

export async function delay(ms: number = MOCK_DELAY): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export const users = {
    test: {
        id: '123',
        email: 'test@example.com',
        password: 'password123',
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ0ZXN0QGV4YW1wbGUuY29tIiwiU3Vic2NyaXB0aW9uVHlwZSI6IlByZW1pdW0iLCJTdWJzY3JpcHRpb25FbmQiOiIyMDI1LTEyLTMxIiwiZXhwIjoxNzE2NDcyNjQ3fQ.d7dJF4JJR7KQkM9jkZiXYx1rX-MDCvNRzAVYDLfFH2I',
        subscriptionType: 'Premium',
        subscriptionEnd: '2025-12-31'
    },
    admin: {
        id: '456',
        email: 'admin@example.com',
        password: 'admin123',
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQ1NiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJhZG1pbkBleGFtcGxlLmNvbSIsIlN1YnNjcmlwdGlvblR5cGUiOiJBZG1pbiIsImV4cCI6MTcxNjQ3MjY0N30.XYZ',
        subscriptionType: 'Admin'
    }
};