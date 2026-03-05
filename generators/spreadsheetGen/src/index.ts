import express from 'express';
import cors from 'cors';
import { createObjectStore } from './storage/createObjectStore.js';
import { makeExportExcelRoute } from './exportRoute.js';


const app = express();

app.use(cors())
app.use(express.json({
    limit: process.env.JSON_LIMIT || '1mb'
}));

const store = createObjectStore();

app.post('/export-excel', makeExportExcelRoute(store));

const port = process.env.PORT ? parseInt(process.env.PORT, 10) : 5301;
app.listen(port, () => {
    console.log(`Server is running on port ${port}`);
});