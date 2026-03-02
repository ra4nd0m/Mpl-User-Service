import type { SubscriptionType } from '$lib/api/adminClient';
import { fetchWithAuth } from './authClient';

export type UploadStatus = 'pending' | 'uploading' | 'complete' | 'error' | 'cancelled';
export type DownloadStatus = 'pending' | 'downloading' | 'complete' | 'error' | 'cancelled';

export interface UploadItem {
    id: string;
    file: File;
    group: string;
    requiredSubscription: SubscriptionType;
    status: UploadStatus;
    abortController: AbortController | null;
}

export interface UserFileMetadata {
    id: string;
    fileName: string;
    group: string;
    requiredSubscription: SubscriptionType;
    uploadedAt: string;
}

export interface UserFile extends UserFileMetadata {
    status: DownloadStatus;
    abortController: AbortController | null;
}

export interface StorageUsage {
    usedBytes: number;
    maxBytes: number;
}

export async function uploadFile(item: UploadItem) {
    const controller = new AbortController();
    item.abortController = controller;
    item.status = 'uploading';

    const formData = new FormData();
    formData.append('file', item.file);
    formData.append('group', item.group);
    formData.append('requiredSubscription', item.requiredSubscription.toString());

    try {
        const resp = await fetchWithAuth('reports/upload', {
            method: 'POST',
            body: formData,
            signal: controller.signal
        });
        if (!resp.ok) {
            throw new Error(resp.statusText);
        }
        item.status = 'complete';
    } catch (err) {
        if (controller.signal.aborted) {
            item.status = 'cancelled';
        } else {
            item.status = 'error';
            console.error('File upload error:', err);
        }
    } finally {
        item.abortController = null;
    }
}

export async function getFilesList(): Promise<UserFileMetadata[] | null> {
    try {
        const resp = await fetchWithAuth(`reports`);
        if (!resp.ok) {
            throw new Error(resp.statusText);
        }

        return await resp.json()
    } catch (err) {
        console.error(`Error while getting the report list ${err}`)
        return null;
    }
}

export async function downloadFile(file: UserFile) {
    const controller = new AbortController();
    file.abortController = controller;
    file.status = 'downloading';

    try {
        const resp = await fetchWithAuth(`reports/${file.id}`, {
            signal: controller.signal
        });

        if (!resp.ok) {
            throw new Error(resp.statusText)
        }

        const blob = await resp.blob();
        const url = URL.createObjectURL(blob);

        const a = document.createElement("a");
        a.href = url;
        a.download = file.fileName;
        a.click();

        URL.revokeObjectURL(url);
        file.status = 'pending';
    } catch (err) {
        if (controller.signal.aborted) {
            file.status = 'cancelled';
        } else {
            file.status = 'error';
            console.error(`File download error: ${err}`)
        }
    } finally {
        file.abortController = null;
    }
}

export async function deleteFile(fileId: string) {
    try {
        const resp = await fetchWithAuth(`reports/${fileId}`, {
            method: 'DELETE'
        });
        if (!resp.ok) {
            throw new Error(resp.statusText);
        }
    } catch (err) {
        console.error(`Error while deleting the file ${err}`)
    }
}

export async function getStorageUsage(): Promise<StorageUsage | null> {
    try {
        const resp = await fetchWithAuth(`reports/storage-usage`);
        if (!resp.ok) {
            throw new Error(resp.statusText);
        }

        return await resp.json()
    } catch (err) {
        console.error(`Error while getting the storage usage ${err}`)
        return null;
    }
}