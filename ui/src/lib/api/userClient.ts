import { delay, ENABLE_MOCKS, mockFavoriteMaterials, mockMaterials } from "$lib/mock";
import { fetchWithAuth } from "./authClient";

export async function getFavorites(): Promise<number[] | null> {
    try {
        if (ENABLE_MOCKS) {
            await delay();
            return mockFavoriteMaterials['123'];
        }
        const resp = await fetchWithAuth('/favorites');
        if (!resp.ok) {
            console.error('Failed to get favorites:', resp.statusText);
            return null;
        }
        return await resp.json();
    } catch (error) {
        console.error('Error during getFavorites:', error);
        return null;
    }
}

export async function addFavorite(id: number): Promise<number[] | null> {
    try {
        if (ENABLE_MOCKS) {
            await delay();
            const currentFavorites = mockFavoriteMaterials['123'];
            if (!currentFavorites.includes(id)) {
                currentFavorites.push(id);
            }
            return currentFavorites;
        }
        const resp = await fetchWithAuth('/favorites', {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ itemId: id })
        });
        if (!resp.ok) {
            console.error('Failed to add favorite:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during addFavorite:', err);
        return null;
    }
}

export async function removeFavorite(id: number): Promise<number[] | null> {
    try {
        if (ENABLE_MOCKS) {
            await delay();
            const currentFavorites = mockFavoriteMaterials['123'];
            const index = currentFavorites.indexOf(id);
            if (index >= 0) {
                currentFavorites.splice(index, 1);
            }
            return currentFavorites;
        }
        const resp = await fetchWithAuth(`/favorites/${id}`, {
            method: 'DELETE'
        });
        if (!resp.ok) {
            console.error('Failed to remove favorite:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during removeFavorite:', err);
        return null;
    }
}

export async function getMaterials(): Promise<Material[] | null> {
    try {
        if (ENABLE_MOCKS) {
            await delay();
            return mockMaterials;
        }
        const resp = await fetchWithAuth('data/materials');
        if (!resp.ok) {
            console.error('Failed to get materials:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getMaterials:', err);
        return null;
    }
}

export async function getOverview()




export interface Material {
    id: number;
    materialName: string;
    source: string;
    deliveryType: string;
    group: string;
    market: string;
    unit: string;
    lastCreatedDate: string | null;
}

export interface MaterialDateMetricReq {
    materialId: number;
    propertyIds: number[];
    startDate: string;
    endDate: string;
}

export interface MaterialDateMetricsResp {
    id: number;
    date: string;
    propsUsed: number[];
    valueAvg: string | null;
    valueMin: string | null;
    valueMax: string | null;
    predWeekly: string | null;
    predMonthly: string | null;
    supply: string | null;
    monthlyAvg: string | null;
}

export interface DateGroupedMaterialValues {
    date: string;
    materialValues: MaterialDateMetricsResp[]
}