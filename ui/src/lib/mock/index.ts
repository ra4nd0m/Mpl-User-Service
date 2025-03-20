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
        password: 'AdminPassword123!',
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQ1NiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJhZG1pbkBleGFtcGxlLmNvbSIsIlN1YnNjcmlwdGlvblR5cGUiOiJBZG1pbiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwiZXhwIjoxNzE2NDcyNjQ3fQ.XYZ',
        subscriptionType: 'Admin'
    }
};

export const mockFavoriteMaterials = {
    '123': [1, 3, 5, 8], // Test user's favorite materials
    '456': [2, 4, 7, 10]  // Admin user's favorite materials
};

export const mockMaterials = [
    {
        id: 1,
        materialName: "Steel Plate A36",
        source: "Domestic",
        deliveryType: "Truck",
        group: "Metal",
        market: "Construction",
        unit: "kg",
        lastCreatedDate: "2025-01-15"
    },
    {
        id: 2,
        materialName: "Aluminum Sheet 6061",
        source: "Import",
        deliveryType: "Ship",
        group: "Metal",
        market: "Aerospace",
        unit: "kg",
        lastCreatedDate: "2025-02-03"
    },
    {
        id: 3,
        materialName: "Polypropylene Pellets",
        source: "Domestic",
        deliveryType: "Rail",
        group: "Plastic",
        market: "Consumer",
        unit: "ton",
        lastCreatedDate: "2025-01-22"
    },
    {
        id: 4,
        materialName: "Glass Fiber",
        source: "Import",
        deliveryType: "Truck",
        group: "Composite",
        market: "Automotive",
        unit: "kg",
        lastCreatedDate: "2025-02-17"
    },
    {
        id: 5,
        materialName: "Copper Wire",
        source: "Domestic",
        deliveryType: "Truck",
        group: "Metal",
        market: "Electronics",
        unit: "m",
        lastCreatedDate: "2025-01-30"
    },
    {
        id: 6,
        materialName: "Pine Lumber",
        source: "Local",
        deliveryType: "Truck",
        group: "Wood",
        market: "Construction",
        unit: "board-ft",
        lastCreatedDate: "2025-02-22"
    },
    {
        id: 7,
        materialName: "Silicon Wafer",
        source: "Import",
        deliveryType: "Air",
        group: "Semiconductor",
        market: "Electronics",
        unit: "piece",
        lastCreatedDate: null
    },
    {
        id: 8,
        materialName: "Cotton Fabric",
        source: "Import",
        deliveryType: "Ship",
        group: "Textile",
        market: "Apparel",
        unit: "yard",
        lastCreatedDate: "2025-03-05"
    },
    {
        id: 9,
        materialName: "Concrete Mix",
        source: "Local",
        deliveryType: "Truck",
        group: "Building",
        market: "Construction",
        unit: "m³",
        lastCreatedDate: "2025-02-28"
    },
    {
        id: 10,
        materialName: "Titanium Alloy",
        source: "Import",
        deliveryType: "Air",
        group: "Metal",
        market: "Medical",
        unit: "kg",
        lastCreatedDate: "2025-03-01"
    }
];

export const sampleData = [
    {
        date: "2025-03-15T00:00:00",
        materialValues: [
            {
                id: 101,
                date: "2025-03-15T00:00:00",
                propsUsed: [1, 2, 3],
                valueAvg: "10.2",
                valueMin: "5.1",
                valueMax: "15.3",
                predWeekly: "10.9",
                predMonthly: "44.8",
                supply: "105",
                monthlyAvg: "9.8",
                materialInfo: {
                    id: 1,
                    materialName: "Steel",
                    deliveryType: "Standard",
                    market: "Construction",
                    unit: "tons"
                }
            },
            {
                id: 102,
                date: "2025-03-15T00:00:00",
                propsUsed: [1, 4, 5],
                valueAvg: "22.4",
                valueMin: "8.6",
                valueMax: "35.2",
                predWeekly: "23.1",
                predMonthly: "89.5",
                supply: "52",
                monthlyAvg: "21.9",
                materialInfo: {
                    id: 2,
                    materialName: "Aluminum",
                    deliveryType: "Express",
                    market: "Manufacturing",
                    unit: "kg"
                }
            },
            {
                id: 103,
                date: "2025-03-15T00:00:00",
                propsUsed: [2, 3, 6],
                valueAvg: "86.3",
                valueMin: "75.8",
                valueMax: "97.2",
                predWeekly: "88.5",
                predMonthly: "355.0",
                supply: "30",
                monthlyAvg: "85.9",
                materialInfo: {
                    id: 3,
                    materialName: "Copper",
                    deliveryType: "Premium",
                    market: "Electronics",
                    unit: "kg"
                }
            },
            {
                id: 104,
                date: "2025-03-15T00:00:00",
                propsUsed: [1, 3, 7],
                valueAvg: "452.6",
                valueMin: "398.5",
                valueMax: "510.9",
                predWeekly: "455.0",
                predMonthly: "1820.0",
                supply: "15",
                monthlyAvg: "450.3",
                materialInfo: {
                    id: 4,
                    materialName: "Titanium",
                    deliveryType: "Special",
                    market: "Aerospace",
                    unit: "kg"
                }
            },
            {
                id: 105,
                date: "2025-03-15T00:00:00",
                propsUsed: [2, 4, 5],
                valueAvg: "28.7",
                valueMin: "25.3",
                valueMax: "32.1",
                predWeekly: "29.2",
                predMonthly: "117.8",
                supply: "85",
                monthlyAvg: "28.5",
                materialInfo: {
                    id: 5,
                    materialName: "Zinc",
                    deliveryType: "Standard",
                    market: "Manufacturing",
                    unit: "kg"
                }
            }
        ]
    },
    {
        date: "2025-03-16T00:00:00",
        materialValues: [
            {
                id: 201,
                date: "2025-03-16T00:00:00",
                propsUsed: [1, 2, 3],
                valueAvg: "10.5",
                valueMin: "5.2",
                valueMax: "15.8",
                predWeekly: "11.2",
                predMonthly: "45.6",
                supply: "100",
                monthlyAvg: "10.1",
                materialInfo: {
                    id: 1,
                    materialName: "Steel",
                    deliveryType: "Standard",
                    market: "Construction",
                    unit: "tons"
                }
            },
            {
                id: 202,
                date: "2025-03-16T00:00:00",
                propsUsed: [1, 4, 5],
                valueAvg: "22.7",
                valueMin: "8.9",
                valueMax: "35.6",
                predWeekly: "23.5",
                predMonthly: "90.1",
                supply: "50",
                monthlyAvg: "22.3",
                materialInfo: {
                    id: 2,
                    materialName: "Aluminum",
                    deliveryType: "Express",
                    market: "Manufacturing",
                    unit: "kg"
                }
            },
            {
                id: 203,
                date: "2025-03-16T00:00:00",
                propsUsed: [2, 3, 6],
                valueAvg: "86.9",
                valueMin: "76.2",
                valueMax: "98.1",
                predWeekly: "89.0",
                predMonthly: "356.8",
                supply: "28",
                monthlyAvg: "86.5",
                materialInfo: {
                    id: 3,
                    materialName: "Copper",
                    deliveryType: "Premium",
                    market: "Electronics",
                    unit: "kg"
                }
            },
            {
                id: 206,
                date: "2025-03-16T00:00:00",
                propsUsed: [2, 6, 8],
                valueAvg: "184.3",
                valueMin: "165.7",
                valueMax: "198.9",
                predWeekly: "186.0",
                predMonthly: "743.5",
                supply: "22",
                monthlyAvg: "182.7",
                materialInfo: {
                    id: 6,
                    materialName: "Nickel",
                    deliveryType: "Premium",
                    market: "Industrial",
                    unit: "kg"
                }
            },
            {
                id: 207,
                date: "2025-03-16T00:00:00",
                propsUsed: [1, 2, 7],
                valueAvg: "7.8",
                valueMin: "6.5",
                valueMax: "9.2",
                predWeekly: "8.0",
                predMonthly: "32.1",
                supply: "250",
                monthlyAvg: "7.6",
                materialInfo: {
                    id: 7,
                    materialName: "Concrete",
                    deliveryType: "Bulk",
                    market: "Construction",
                    unit: "cubic meter"
                }
            },
            {
                id: 208,
                date: "2025-03-16T00:00:00",
                propsUsed: [1, 3, 8],
                valueAvg: "24.6",
                valueMin: "20.8",
                valueMax: "29.7",
                predWeekly: "25.2",
                predMonthly: "102.5",
                supply: "120",
                monthlyAvg: "24.1",
                materialInfo: {
                    id: 8,
                    materialName: "Wood",
                    deliveryType: "Standard",
                    market: "Construction",
                    unit: "cubic meter"
                }
            }
        ]
    },
    // The rest of the data follows the same pattern
];
