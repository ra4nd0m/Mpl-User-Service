import { delay, ENABLE_MOCKS, mockFavoriteMaterials } from "$lib/mock";
import type { WidgetSettings } from "$lib/stores/widgetSettingStore";
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
        const resp = await fetchWithAuth(`/favorites/${id}`, {
            method: 'PUT'
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

export async function setFavourites(ids: number[]): Promise<number[] | null> {
    try {
        const resp = await fetchWithAuth('/favorites', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ids)
        });
        if (!resp.ok) {
            console.error('Failed to set favorites:', resp.statusText);
            return null;
        }
        return await resp.json();
    } catch (err) {
        console.error('Error during setFavourites:', err);
        return null;
    }
}

export async function getMaterials(): Promise<Material[] | null> {
    try {
        const resp = await fetchWithAuth('data/filtered/materials');
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

export async function getMaterialsByGroup(id: number): Promise<Material[] | null> {
    try {
        const resp = await fetchWithAuth(`data/filtered/materials/bygroup/${id}`);
        if (!resp.ok) {
            console.error('Failed to get materials by group:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getMaterialsByGroup:', err);
        return null;
    }
}

export async function getMaterialGroups(): Promise<{ id: number, name: string }[] | null> {
    try {
        const resp = await fetchWithAuth('data/filtered/materialgroups');
        if (!resp.ok) {
            console.error('Failed to get material groups:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getMaterialGroups:', err);
        return null;
    }
}

export async function getSources(): Promise<{ id: number, name: string }[] | null> {
    try {
        const resp = await fetchWithAuth('data/filtered/sources');
        if (!resp.ok) {
            console.error('Failed to get sources:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getSources:', err);
        return null;
    }
}

export async function getUnits(): Promise<{ id: number, name: string }[] | null> {
    try {
        const resp = await fetchWithAuth('data/filtered/units');
        if (!resp.ok) {
            console.error('Failed to get units:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getUnits:', err);
        return null;
    }
}

export async function getPropertiesForDropdown(): Promise<{ id: number, name: string }[] | null> {
    try {
        const resp = await fetchWithAuth('data/filtered/properties/dropdown');
        if (!resp.ok) {
            console.error('Failed to get properties:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getUnits:', err);
        return null;
    }
}


export async function getOverview(materialIds: number[], propertyIds: number[], startDate: string, endDate: string): Promise<DateGroupedMaterialValues[] | null> {
    try {
        const reqsts: MaterialDateMetricReq[] = materialIds.map(id => ({
            materialId: id,
            propertyIds,
            startDate,
            endDate
        }));

        const resp = await fetchWithAuth('data/filtered/materialvalues/overview', {
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

export async function getMaterialInfo(materialId: number | { materialId: number }): Promise<Material | null> {
    try {
        const actualMaterialId = typeof materialId === 'object' && materialId !== null ?
            (materialId as { materialId: number }).materialId : materialId;
        const resp = await fetchWithAuth(`data/filtered/materials/${actualMaterialId}`);
        if (!resp.ok) {
            console.error('Failed to get material info:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getMaterialInfo:', err);
        return null;
    }
}

export async function getMaterialDateMetrics(materialId: number | { materialId: number }, propertyIds: number[], startDate: string, endDate: string, aggregates: string[] = []): Promise<MaterialDateMetricsResp[] | null> {
    try {
        const actualMaterialId = typeof materialId === 'object' && materialId !== null ?
            (materialId as { materialId: number }).materialId : materialId;
        const req: MaterialDateMetricReq = {
            materialId: actualMaterialId,
            propertyIds,
            startDate,
            endDate,
            aggregates
        }
        const resp = await fetchWithAuth('data/filtered/materialvalues/daterange', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(req)
        });
        if (!resp.ok) {
            console.error('Failed to get material date metrics:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data;
    } catch (err) {
        console.error('Error during getMaterialDateMetrics:', err);
        return null;
    }
}

export async function getMaterialSpreadsheet(spreadsheetReq: SpreadsheetReq): Promise<void | null> {
    try {
        const resp = await fetchWithAuth('generator/spreadsheet/export-excel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(spreadsheetReq)
        });
        if (!resp.ok) {
            console.error('Failed to get material spreadsheet:', resp.statusText);
            return null;
        }
        const blob = await resp.blob();
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${spreadsheetReq.materialName}-${spreadsheetReq.unit}-${spreadsheetReq.deliveryType}-${spreadsheetReq.market}.xlsx`;
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (err) {
        console.error('Error during getMaterialSpreadsheet:', err);
        return null;
    }
}

export async function getUserSettings(): Promise<WidgetSettings | null> {
    try {
        const resp = await fetchWithAuth('settings');
        if (!resp.ok) {
            console.error('Failed to get user settings:', resp.statusText);
            return null;
        }
        const data = await resp.json();
        return data as WidgetSettings;
    } catch (err) {
        console.error('Error during getUserSettings:', err);
        return null;
    }
}

export async function updateUserSettings(settings: WidgetSettings): Promise<void | null> {
    try {
        const resp = await fetchWithAuth('settings', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(settings)
        });
        if (!resp.ok) {
            console.error('Failed to update user settings:', resp.statusText);
            return null;
        }
    } catch (err) {
        console.error('Error during updateUserSettings:', err);
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
    changePercent: string | null;
    latestAvgValue?: number | null;
    latestMinValue?: number | null;
    latestMaxValue?: number | null;
    avalibleProps: number[];

}

export interface MaterialDateMetricReq {
    materialId: number;
    propertyIds: number[];
    startDate: string;
    endDate: string;
    aggregates?: string[]; // Optional, e.g., ['weekly', 'monthly', 'quarterly', 'yearly']
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
    weeklyAvg: string | null;
    monthlyAvg: string | null;
    quarterlyAvg: string | null;
    yearlyAvg: string | null;
    materialInfo: CompactMaterialInfo;
}

export interface DateGroupedMaterialValues {
    date: string;
    materialValues: MaterialDateMetricsResp[]
}

export interface SpreadsheetReq {
    materialName: string;
    unit: string;
    deliveryType: string;
    market: string;
    data: SpreadsheetReqData[] | SpreadsheetReqAvgData[];
    type: 'full' | 'weekly' | 'monthly' | 'quarterly' | 'yearly';
}

export interface SpreadsheetReqData {
    date: string;
    valueMin: string | null;
    valueAvg: string | null;
    valueMax: string | null;
    predWeekly: string | null;
    predMonthly: string | null;
    supply: string | null;
    propsUsed: number[];
    weeklyAvg: string | null;
    monthlyAvg: string | null;
    quarterlyAvg: string | null;
    yearlyAvg: string | null;
}

export interface IdNamePair {
    id: number;
    name: string;
}

export interface SpreadsheetReqAvgData {
    date: string;
    value: string | null;
}