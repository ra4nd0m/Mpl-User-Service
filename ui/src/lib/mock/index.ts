import { browser } from "$app/environment";

export const ENABLE_MOCKS = browser && (import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === 'true');

export const MOCK_DELAY = 800;



export async function delay(ms: number = MOCK_DELAY): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export const users = {
    test: {
        id: '123',
        email: 'test@example.com',
        password: 'password123',
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ0ZXN0QGV4YW1wbGUuY29tIiwiU3Vic2NyaXB0aW9uVHlwZSI6IlByZW1pdW0iLCJTdWJzY3JpcHRpb25FbmQiOiIyMDI1LTEyLTMxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiVXNlciIsImV4cCI6MTcxNjQ3MjY0N30.d7dJF4JJR7KQkM9jkZiXYx1rX-MDCvNRzAVYDLfFH2I',
        subscriptionType: 'Premium',
        subscriptionEnd: '2025-12-31'
    },
    admin: {
        id: '456',
        email: 'admin@example.com',
        password: 'admin123',
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQ1NiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJhZG1pbkBleGFtcGxlLmNvbSIsIlN1YnNjcmlwdGlvblR5cGUiOiJBZG1pbiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNzE2NDcyNjQ3fQ.XYZ',
        subscriptionType: 'Admin'
    }
};

export const favoriteMaterials = {
    '123': [1, 3, 5, 8], // Test user's favorite materials
    '456': [2, 4, 7, 10]  // Admin user's favorite materials
};

export const materials = [
    {
        Id: 1,
        MaterialName: "Steel Plate A36",
        Source: "Domestic",
        DeliveryType: "Truck",
        Group: "Metal",
        Market: "Construction",
        Unit: "kg",
        LastCreatedDate: "2025-01-15"
    },
    {
        Id: 2,
        MaterialName: "Aluminum Sheet 6061",
        Source: "Import",
        DeliveryType: "Ship",
        Group: "Metal",
        Market: "Aerospace",
        Unit: "kg",
        LastCreatedDate: "2025-02-03"
    },
    {
        Id: 3,
        MaterialName: "Polypropylene Pellets",
        Source: "Domestic",
        DeliveryType: "Rail",
        Group: "Plastic",
        Market: "Consumer",
        Unit: "ton",
        LastCreatedDate: "2025-01-22"
    },
    {
        Id: 4,
        MaterialName: "Glass Fiber",
        Source: "Import",
        DeliveryType: "Truck",
        Group: "Composite",
        Market: "Automotive",
        Unit: "kg",
        LastCreatedDate: "2025-02-17"
    },
    {
        Id: 5,
        MaterialName: "Copper Wire",
        Source: "Domestic",
        DeliveryType: "Truck",
        Group: "Metal",
        Market: "Electronics",
        Unit: "m",
        LastCreatedDate: "2025-01-30"
    },
    {
        Id: 6,
        MaterialName: "Pine Lumber",
        Source: "Local",
        DeliveryType: "Truck",
        Group: "Wood",
        Market: "Construction",
        Unit: "board-ft",
        LastCreatedDate: "2025-02-22"
    },
    {
        Id: 7,
        MaterialName: "Silicon Wafer",
        Source: "Import",
        DeliveryType: "Air",
        Group: "Semiconductor",
        Market: "Electronics",
        Unit: "piece",
        LastCreatedDate: null
    },
    {
        Id: 8,
        MaterialName: "Cotton Fabric",
        Source: "Import",
        DeliveryType: "Ship",
        Group: "Textile",
        Market: "Apparel",
        Unit: "yard",
        LastCreatedDate: "2025-03-05"
    },
    {
        Id: 9,
        MaterialName: "Concrete Mix",
        Source: "Local",
        DeliveryType: "Truck",
        Group: "Building",
        Market: "Construction",
        Unit: "m³",
        LastCreatedDate: "2025-02-28"
    },
    {
        Id: 10,
        MaterialName: "Titanium Alloy",
        Source: "Import",
        DeliveryType: "Air",
        Group: "Metal",
        Market: "Medical",
        Unit: "kg",
        LastCreatedDate: "2025-03-01"
    }
];