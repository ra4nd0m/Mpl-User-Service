import { ExportExcelRequestBody } from "../types"

type CanonicalExport = {
    type: ExportExcelRequestBody['type'];
    materialName: string | null;
    unit: string | null;
    deliveryType: string | null;
    market: string | null;
    data: Array<Record<string, unknown>>;
}

export function canonicalizeExport(body: ExportExcelRequestBody): CanonicalExport {
    const data = body.data.map((row) => {
        if ("propsUsed" in row) {
            const propsUsed = [...row.propsUsed].sort((a, b) => a - b);

            return {
                date: new Date(row.date).toISOString(),
                propsUsed,
                valueMin: row.valueMin ?? null,
                valueAvg: row.valueAvg ?? null,
                valueMax: row.valueMax ?? null,
                predWeekly: row.predWeekly ?? null,
                predMonthly: row.predMonthly ?? null,
                supply: row.supply ?? null,
                weeklyAvg: row.weeklyAvg ?? null,
                monthlyAvg: row.monthlyAvg ?? null,
                quarterlyAvg: row.quarterlyAvg ?? null,
                yearlyAvg: row.yearlyAvg ?? null
            };
        }

        return {
            date: new Date(row.date).toISOString(),
            value: row.value ?? null
        };
    });

    return {
        type: body.type,
        materialName: body.materialName ?? null,
        unit: body.unit ?? null,
        deliveryType: body.deliveryType ?? null,
        market: body.market ?? null,
        data
    }
}