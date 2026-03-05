import { createReadStream, createWriteStream, promises as fsp } from "node:fs";
import { dirname, join } from "node:path";
import { pipeline } from "node:stream/promises";
import type { GetResult, ObjectStore } from "./ObjectStore.js";

export class DiskObjectStore implements ObjectStore {
    constructor(private readonly root: string) { }

    async put(key: string, body: NodeJS.ReadableStream, contentType: string): Promise<void> {
        const path = join(this.root, key);
        await fsp.mkdir(dirname(path), { recursive: true });

        const out = createWriteStream(path);
        await pipeline(body, out);
    }

    async get(key: string): Promise<GetResult> {
        const path = join(this.root, key);

        const stat = await fsp.stat(path);

        return {
            stream: createReadStream(path),
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            contentLength: stat.size
        };
    }

    async del(key: string): Promise<void> {
        const path = join(this.root, key);
        await fsp.rm(path, { force: true });
    }
}