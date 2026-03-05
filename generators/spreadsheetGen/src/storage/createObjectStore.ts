import { DiskObjectStore } from "./DiskObjectStore.js";
import { ObjectStore } from "./ObjectStore.js";

// For future providers that demand config set up, like S3
function required(name: string): string {
    const v = process.env[name];
    if (!v) throw new Error(`Missing env var: ${name}`);
    return v;
}

export function createObjectStore(): ObjectStore {
    const provider = process.env.STORAGE_PROVIDER ?? "disk";

    switch (provider) {
        case "disk":
            const root = process.env.DISK_ROOT ?? "./data/exports";
            return new DiskObjectStore(root);
        default:
            throw new Error(`Unknown storage provider: ${provider}`);
    }
}