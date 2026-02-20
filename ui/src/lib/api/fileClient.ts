import type { SubscriptionType } from '$lib/api/adminClient';
import { fetchWithAuth } from './authClient';

export type UploadStatus = 'pending' | 'uploading' | 'complete' | 'error' | 'canceled';

export interface UploadItem {
    id: string;
    file: File;
    requiredSubscription: SubscriptionType;
    status: UploadStatus;
    abortController: AbortController | null;
}

export async function uploadFile(item: UploadItem) {
    const controller = new AbortController();
    item.abortController = controller;
    item.status = 'uploading';

    const formData = new FormData();
    formData.append('file', item.file);

    try {
        const resp = await fetchWithAuth('reports/upload', {
            method: 'POST',
            body: formData,
            signal: controller.signal
        });
        if (!resp.ok) {
            console.error('File upload failed:', resp.statusText);
        }
    } catch (err) {
        if (controller.signal.aborted) {
            item.status = 'canceled';
        } else {
            item.status = 'error';
            console.error('File upload error:', err);
        }
    } finally {
        item.abortController = null;
    }
}