import { delay, users } from ".";

export type LoginCredentials = {
    email: string;
    password: string;
    rememberMe: boolean;
};
export type LoginResponse = {
    success: boolean;
    token?: string;
    error?: string;
}

export async function mockLogin(credentials: LoginCredentials): Promise<LoginResponse> {
    await delay();

    const user = Object.values(users).find(u => u.email == credentials.email);

    if (!user || user.password !== credentials.password) {
        return { success: false, error: 'Invalid email or password' };
    };
    return { success: true, token: user.token };
}

export async function mockRefreshToken(): Promise<string | null> {
    await delay();

    return users.test.token;
}

export async function mockLogout(): Promise<boolean> {
    await delay();
    return true;
}
