import crypto from "node:crypto";
import { ExportExcelRequestBody } from "../types.js";
import { canonicalizeExport } from "./canonicalizeExport.js";

const EXPORT_VERSION = "excel-export-v1";

export function makeKeyFromBody(body: ExportExcelRequestBody): string {
    const canonical = canonicalizeExport(body);
    const json = JSON.stringify(canonical);
    const hash = crypto
        .createHash("sha256")
        .update(EXPORT_VERSION)
        .update("\n")
        .update(json)
        .digest("hex");

    return `${EXPORT_VERSION}/${hash}.xlsx`;
}