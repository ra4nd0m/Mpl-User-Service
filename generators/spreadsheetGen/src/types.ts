export interface ExportExcelRequestBody {
    materialName?: string;
    unit?: string;
    deliveryType?: string;
    market?: string;
    data: inputData[] | avgData[];
    type: 'full' | 'weekly' | 'monthly' | 'quarterly' | 'yearly';
}

export type inputData = {
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

export type avgData = {
    date: string;
    value: string | null;
}

export type Rows = (string | number | null)[][];