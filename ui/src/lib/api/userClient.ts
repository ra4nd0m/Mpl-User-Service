import { delay, ENABLE_MOCKS, mockFavoriteMaterials, mockMaterials, sampleData } from "$lib/mock";
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

export async function getOverview(materialIds: number[], propertyIds: number[], startDate: string, endDate: string): Promise<DateGroupedMaterialValues[] | null> {
    try {
        if (ENABLE_MOCKS) {
            await delay();

            return sampleData.filter(entry => {
                const entryDate = new Date(entry.date);
                const start = new Date(startDate);
                const end = new Date(endDate);

                return entryDate >= start && entryDate <= end &&
                    entry.materialValues.some(mv =>
                        materialIds.includes(mv.materialInfo.id) &&
                        propertyIds.some(pid => mv.propsUsed.includes(pid))
                    );
            });

        }
        const reqsts: MaterialDateMetricReq[] = materialIds.map(id => ({
            materialId: id,
            propertyIds,
            startDate,
            endDate
        }));
        
        const resp = await fetchWithAuth('data/materialvalues/overview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(reqsts)
        });

        if (!resp.ok) {
            console.error('Failed to get overview:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getOverview:', err);
        return null;
    }
}




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

export interface CompactMaterialInfo {
    id: number;
    materialName: string;
    deliveryType: string;
    market: string;
    unit: string;
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
    materialInfo: CompactMaterialInfo;
}

export interface DateGroupedMaterialValues {
    date: string;
    materialValues: MaterialDateMetricsResp[]
}