import * as XLSX from "xlsx";
import { RequestHandler } from "express";
import { ObjectStore } from "./storage/ObjectStore.js";
import { avgData, ExportExcelRequestBody, inputData, Rows } from "./types.js";
import { makeKeyFromBody } from "./caching/key.js";
import { inFlight } from "./caching/inflight.js";
import { Readable } from "stream";

const XLSX_MINE =
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

function sanitizeFilename(name: string): string {
    // Remove control characters (including newlines, tabs, etc.)
    return name.replace(/[\x00-\x1F\x7F]/g, "").replace(/\s+/g, "_");
}

function encodeFilenameForHeader(filename: string): string {
    const sanitized = sanitizeFilename(filename);
    // Encode for RFC 5987
    const encoded = encodeURIComponent(sanitized);
    // Provide both ASCII fallback and UTF-8 encoded version
    const asciiFallback = sanitized.replace(/[^\x20-\x7E]/g, "_");
    return `filename="${asciiFallback}"; filename*=UTF-8''${encoded}`;
}

export function makeExportExcelRoute(store: ObjectStore): RequestHandler {
    return async (req, res) => {
        try {
            const body = req.body as ExportExcelRequestBody;

            if (!body.data || !Array.isArray(body.data) || body.data.length === 0) {
                res.status(400).json({ error: "Invalid data format" });
                return;
            }

            const key = makeKeyFromBody(body);

            // try cache, service throws if miss
            try {
                const cached = await store.get(key);

                const filename = `${body.materialName || "price_data"}_${body.market || ""}.xlsx`;

                res.setHeader("Content-Type", cached.contentType || XLSX_MINE);
                res.setHeader("Content-Disposition", `attachment; ${encodeFilenameForHeader(filename)}`);

                cached.stream.pipe(res);
                return;
            } catch {
                // cache miss
            }

            // Handle cache miss withh per-process single-flight
            // Check if the generation is already in process
            let p = inFlight.get(key);
            // If not, generate
            if (!p) {
                p = (async () => {
                    const buffer = buildWorkbookBuffer(body);
                    await store.put(key, Readable.from(buffer), XLSX_MINE);
                    return buffer;
                })().finally(() => inFlight.delete(key));

                inFlight.set(key, p);
            }

            const buffer = await p;

            const filename = `${body.materialName || "price_data"}_${body.market || ""}.xlsx`;

            res.setHeader("Content-Type", XLSX_MINE);
            res.setHeader("Content-Disposition", `attachment; ${encodeFilenameForHeader(filename)}`);
            res.send(buffer);

        } catch (err) {
            console.error("Excel generation failed", err);
            if (!res.headersSent) res.status(500).json({ error: "Excel generation failed" });
        }
    };
}

function buildWorkbookBuffer(body: ExportExcelRequestBody): Buffer {
    const { materialName, unit, deliveryType, market, data, type } = body;

    const title = `${type === 'full' ? '' : type + ' '}Price Data Report`;
    const materialInfo = `Material: ${materialName || 'N/A'} | Unit: ${unit || 'N/A'} | Delivery: ${deliveryType || 'N/A'} | Market: ${market || 'N/A'}`;

    // Define column headers
    const headers = ['Date'];
    if (type === 'full') {
        const example = 'propsUsed' in data[0] ? data[0].propsUsed : [];

        if (example.includes(1)) headers.push('Average Price');
        if (example.includes(2)) headers.push('Min Price');
        if (example.includes(3)) headers.push('Max Price');
        if (example.includes(4)) headers.push('Weekly Forecast');
        if (example.includes(5)) headers.push('Monthly Forecast');
        if (example.includes(6)) headers.push('Supply');
        if (example.includes(-1)) headers.push('Weekly Average');
        if (example.includes(-2)) headers.push('Monthly Average');
        if (example.includes(-3)) headers.push('Quarterly Average');
        if (example.includes(-4)) headers.push('Yearly Average');
    } else {
        headers.push('Value');
    }

    // Create sheet with title and info first, then data
    const rows: Rows = assembleSpreadsheet(title, materialInfo, headers, data);

    const sheet = XLSX.utils.aoa_to_sheet(rows);

    // Merge cells for title and info
    if (!sheet['!merges']) sheet['!merges'] = [];
    sheet['!merges'].push(
        { s: { r: 0, c: 0 }, e: { r: 0, c: headers.length - 1 } },
        { s: { r: 1, c: 0 }, e: { r: 1, c: headers.length - 1 } }
    );

    // Set column widths
    const colWidths = headers.map(() => ({ wch: 15 }));
    sheet['!cols'] = colWidths;

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, sheet, 'Price History');

    // Add workbook properties with metadata
    workbook.Props = {
        Title: "Price Data Report",
        Subject: materialName,
        Author: "MPL User Service",
        CreatedDate: new Date()
    };

    return XLSX.write(workbook, { type: "buffer", bookType: "xlsx" }) as Buffer;
}

function assembleSpreadsheet(title: string, materialInfo: string, headers: string[], data: inputData[] | avgData[]): Rows {
    const rows: Rows = [
        [title],
        [materialInfo],
        [], // Empty row as separator
        headers
    ];

    for (const row of data) {
        const outRow: (string | number | null)[] = [];

        if ('propsUsed' in row) {
            outRow.push(new Date(row.date).toLocaleDateString('ru-RU'));
            if (row.propsUsed.includes(1)) outRow.push(Number(row.valueAvg) || null);
            if (row.propsUsed.includes(2)) outRow.push(Number(row.valueMin) || null);
            if (row.propsUsed.includes(3)) outRow.push(Number(row.valueMax) || null);
            if (row.propsUsed.includes(4)) outRow.push(Number(row.predWeekly) || null);
            if (row.propsUsed.includes(5)) outRow.push(Number(row.predMonthly) || null);
            if (row.propsUsed.includes(6)) outRow.push(Number(row.supply) || null);
            if (row.propsUsed.includes(-1)) outRow.push(Number(row.weeklyAvg) || null);
            if (row.propsUsed.includes(-2)) outRow.push(Number(row.monthlyAvg) || null);
            if (row.propsUsed.includes(-3)) outRow.push(Number(row.quarterlyAvg) || null);
            if (row.propsUsed.includes(-4)) outRow.push(Number(row.yearlyAvg) || null);
        } else {
            outRow.push(row.date);
            outRow.push(Number(row.value) || null);
        }

        rows.push(outRow);
    }

    return rows;
}