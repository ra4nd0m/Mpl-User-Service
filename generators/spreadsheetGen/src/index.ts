import * as XLSX from 'xlsx';
import express, { Request, Response, RequestHandler } from 'express';
import { join } from 'path';
import { tmpdir } from 'os';
import { randomUUID } from 'crypto';

const app = express();
const port = 3001;

app.use(express.json());

app.post('/export-excel', ((req: Request, res: Response) => {
    const { materialName, unit, deliveryType, market, data } = req.body;

    if (!data || !Array.isArray(data) || data.length === 0) {
        return res.status(400).json({ error: 'Invalid data format' });
    }
    const title = `Price Data Report`;
    const materialInfo = `Material: ${materialName || 'N/A'} | Unit: ${unit || 'N/A'} | Delivery: ${deliveryType || 'N/A'} | Market: ${market || 'N/A'}`;

    // Define column headers
    const headers = ['Date'];
    const example = data[0]?.propsUsed || [];

    if (example.includes(1)) headers.push('Average Price');
    if (example.includes(2)) headers.push('Min Price');
    if (example.includes(3)) headers.push('Max Price');
    if (example.includes(4)) headers.push('Weekly Forecast');
    if (example.includes(5)) headers.push('Monthly Forecast');
    if (example.includes(6)) headers.push('Supply');

    // Create sheet with title and info first, then data
    const rows: (string | number | null)[][] = [
        [title],
        [materialInfo],
        [], // Empty row as separator
        headers
    ];

    for (const row of data) {
        const outRow: (string | number | null)[] = [new Date(row.date).toLocaleDateString('ru-RU')];

        if (row.propsUsed.includes(1)) outRow.push(Number(row.valueAvg) || null);
        if (row.propsUsed.includes(2)) outRow.push(Number(row.valueMin) || null);
        if (row.propsUsed.includes(3)) outRow.push(Number(row.valueMax) || null);
        if (row.propsUsed.includes(4)) outRow.push(Number(row.predWeekly) || null);
        if (row.propsUsed.includes(5)) outRow.push(Number(row.predMonthly) || null);
        if (row.propsUsed.includes(6)) outRow.push(Number(row.supply) || null);

        rows.push(outRow);
    }

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

    const tmpPath = join(tmpdir(), `export-${randomUUID()}.xlsx`);
    XLSX.writeFile(workbook, tmpPath);

    const filename = `${materialName || 'price_data'}_${market || ''}.xlsx`.replace(/\s+/g, '_');
    res.download(tmpPath, filename, (err) => {
        if (err) console.error('Download error:', err);
    });
}) as RequestHandler);

app.listen(port, () => {
    console.log(`Excel export server running at http://localhost:${port}`);

});