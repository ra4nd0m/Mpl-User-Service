<script lang="ts">
	import { onMount } from 'svelte';
	import {
		uploadFile,
		getStorageUsage,
		type UploadItem,
		type StorageUsage
	} from '$lib/api/fileClient';
	import { SubscriptionType } from '$lib/api/adminClient';
	import { m } from '$lib/i18n';

	let { refreshReports, existingGroups }: { refreshReports: () => void; existingGroups: string[] } = $props();

	const subscriptionOptions = (Object.entries(SubscriptionType) as [string, number][]).filter(
		([, v]) => typeof v === 'number'
	);

	const files = $state<UploadItem[]>([]);
	let storageUsage = $state<StorageUsage | null>(null);

	const pendingBytes = $derived(
		files
			.filter((f) => f.status === 'pending' || f.status === 'error')
			.reduce((sum, f) => sum + f.file.size, 0)
	);

	const storageExceeded = $derived(
		storageUsage !== null && storageUsage.usedBytes + pendingBytes > storageUsage.maxBytes
	);

	const usagePercent = $derived(
		storageUsage ? Math.min((storageUsage.usedBytes / storageUsage.maxBytes) * 100, 100) : 0
	);

	const projectedPercent = $derived(
		storageUsage
			? Math.min(((storageUsage.usedBytes + pendingBytes) / storageUsage.maxBytes) * 100, 100)
			: 0
	);

	function formatBytes(bytes: number): string {
		if (bytes < 1024) return `${bytes} B`;
		if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
		if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
		return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
	}

	const canPublish = $derived(
		!storageExceeded &&
		files.some((f) => f.status === 'pending' || f.status === 'error') &&
		files
			.filter((f) => f.status === 'pending' || f.status === 'error')
			.every((f) => f.group.trim() !== '')
	);
	const isUploading = $derived(files.some((f) => f.status === 'uploading'));

	onMount(async () => {
		storageUsage = await getStorageUsage();
	});

	function addFiles(fileList: FileList) {
		for (const file of fileList) {
			if (file.type !== 'application/pdf') {
				alert(m.add_files_not_pdf_error({ fileName: file.name }));
				continue;
			}

			files.push({
				id: crypto.randomUUID(),
				file,
				group: '',
				requiredSubscription: SubscriptionType.Premium,
				status: 'pending',
				abortController: null
			});
		}
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		if (e.dataTransfer?.files) {
			addFiles(e.dataTransfer.files);
		}
	}

	function handlePicker(e: Event) {
		const input = e.target as HTMLInputElement;
		if (input.files) {
			addFiles(input.files);
			input.value = '';
		}
	}

	function handleRemove(id: string) {
		const index = files.findIndex((f) => f.id === id);
		if (index === -1) return;

		const item = files[index];

		if (item.abortController) {
			item.abortController.abort();
		}

		files.splice(index, 1);
	}

	async function publishFiles() {
		for (const item of files) {
			if (item.status === 'pending' || item.status === 'error') {
				await uploadFile(item);
			}
		}
		storageUsage = await getStorageUsage();
		refreshReports();
	}
</script>

<div class="modal-content">
	<div
		class="dropzone"
		role="button"
		tabindex="0"
		ondrop={handleDrop}
		ondragover={(e) => e.preventDefault()}
	>
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="48"
			height="48"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
		>
			<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
			<polyline points="17 8 12 3 7 8"></polyline>
			<line x1="12" y1="3" x2="12" y2="15"></line>
		</svg>
		<p>{m.add_files_drag_drop()}</p>
		<p class="or">{m.add_files_or()}</p>
		<label for="file-input" class="file-input-label">{m.add_files_browse_files()}</label>
		<input
			id="file-input"
			type="file"
			multiple
			accept="application/pdf"
			onchange={handlePicker}
			class="file-input-hidden"
		/>
	</div>

	{#if storageUsage !== null}
		<div class="storage-bar-section" class:exceeded={storageExceeded}>
			<div class="storage-bar-header">
				<span class="storage-label">{m.add_files_storage()}</span>
				<span class="storage-values">
					{formatBytes(storageUsage.usedBytes)}
					{#if pendingBytes > 0}
						<span class="pending-delta"> + {formatBytes(pendingBytes)} {m.add_files_pending()}</span>
					{/if}
					/ {formatBytes(storageUsage.maxBytes)}
				</span>
			</div>
			<div class="storage-track">
				<div class="storage-fill" style="width: {usagePercent}%"></div>
				{#if pendingBytes > 0 && !storageExceeded}
					<div
						class="storage-fill pending"
						style="width: {projectedPercent - usagePercent}%; left: {usagePercent}%"
					></div>
				{/if}
			</div>
			{#if storageExceeded}
				<p class="storage-exceeded-msg">{m.add_files_storage_exceeded()}</p>
			{/if}
		</div>
	{/if}

	{#if files.length > 0}
		<div class="files-section">
			<h4>{m.add_files_files_to_upload()}</h4>
			<div class="files-table">
				{#each files as item}
					<div class="file-row" class:complete={item.status === 'complete'}>
						<div class="file-name">
							<svg
								xmlns="http://www.w3.org/2000/svg"
								width="20"
								height="20"
								viewBox="0 0 24 24"
								fill="none"
								stroke="currentColor"
								stroke-width="2"
							>
								<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
								<polyline points="14 2 14 8 20 8"></polyline>
							</svg>
							<strong>{item.file.name}</strong>
						</div>

						<input
							list="groups-datalist"
							bind:value={item.group}
							disabled={item.status === 'uploading' || item.status === 'complete'}
						placeholder={m.add_files_group_required()}
						class="group-input"
						class:group-missing={item.group.trim() === '' && item.status !== 'complete'}
					/>

						<select
							bind:value={item.requiredSubscription}
							disabled={item.status === 'uploading'}
							class="subscription-select"
						>
							{#each subscriptionOptions as [label, value]}
								<option {value}>{label}</option>
							{/each}
						</select>

						<span class="status status-{item.status}">
							{#if item.status === 'pending'}
							{m.add_files_status_pending()}
						{:else if item.status === 'uploading'}
							{m.add_files_status_uploading()}
						{:else if item.status === 'complete'}
							{m.add_files_status_complete()}
						{:else if item.status === 'error'}
							{m.add_files_status_error()}
						{:else if item.status === 'cancelled'}
							{m.add_files_status_cancelled()}
						{/if}
					</span>

					<button
						class="btn-remove"
						onclick={() => handleRemove(item.id)}
						disabled={item.status === 'complete'}
						title={m.add_files_remove_file()}
						aria-label={m.add_files_remove_file()}
					>
							<svg
								xmlns="http://www.w3.org/2000/svg"
								width="18"
								height="18"
								viewBox="0 0 24 24"
								fill="none"
								stroke="currentColor"
								stroke-width="2"
							>
								<line x1="18" y1="6" x2="6" y2="18"></line>
								<line x1="6" y1="6" x2="18" y2="18"></line>
							</svg>
						</button>
					</div>
				{/each}
			</div>

			<div class="actions">
				{#if !isUploading}
					<button class="btn btn-primary" onclick={publishFiles} disabled={!canPublish}>
						{m.add_files_upload_all()}
					</button>
				{:else}
					<button
						class="btn btn-secondary"
						onclick={() =>
							files.forEach((f) => f.status === 'uploading' && f.abortController?.abort())}
					>
						{m.add_files_cancel_upload()}
					</button>
				{/if}
			</div>
		</div>
	{/if}
</div>

<datalist id="groups-datalist">
	{#each existingGroups as group}
		<option value={group}></option>
	{/each}
</datalist>

<style>
	.modal-content {
		padding: 1rem;
	}

	.storage-bar-section {
		margin-bottom: 1.25rem;
		padding: 0.75rem 1rem;
		background-color: #f7fafc;
		border: 1px solid #e2e8f0;
		border-radius: 8px;
	}

	.storage-bar-section.exceeded {
		background-color: #fff5f5;
		border-color: #fc8181;
	}

	.storage-bar-header {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		margin-bottom: 0.5rem;
	}

	.storage-label {
		font-size: 0.875rem;
		font-weight: 600;
		color: #4a5568;
	}

	.storage-values {
		font-size: 0.8rem;
		color: #718096;
	}

	.pending-delta {
		color: #d69e2e;
		font-weight: 500;
	}

	.storage-track {
		position: relative;
		height: 8px;
		background-color: #e2e8f0;
		border-radius: 4px;
		overflow: hidden;
	}

	.storage-fill {
		position: absolute;
		top: 0;
		left: 0;
		height: 100%;
		background-color: #4299e1;
		border-radius: 4px;
		transition: width 0.3s ease;
	}

	.storage-bar-section.exceeded .storage-fill {
		background-color: #e53e3e;
	}

	.storage-fill.pending {
		position: absolute;
		background-color: #ecc94b;
		opacity: 0.8;
	}

	.storage-exceeded-msg {
		margin: 0.5rem 0 0;
		font-size: 0.8rem;
		color: #e53e3e;
		font-weight: 500;
	}

	.dropzone {
		border: 2px dashed #cbd5e0;
		border-radius: 8px;
		padding: 3rem 2rem;
		text-align: center;
		background-color: #f7fafc;
		cursor: pointer;
		transition: all 0.3s ease;
		margin-bottom: 2rem;
	}

	.dropzone:hover {
		border-color: #4299e1;
		background-color: #ebf8ff;
	}

	.dropzone:focus {
		outline: none;
		border-color: #4299e1;
		box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.1);
	}

	.dropzone svg {
		color: #718096;
		margin: 0 auto 1rem;
		transition: color 0.3s ease;
	}

	.dropzone:hover svg {
		color: #4299e1;
	}

	.dropzone p {
		margin: 0.5rem 0;
		color: #4a5568;
		font-size: 1rem;
	}

	.dropzone p.or {
		color: #a0aec0;
		font-size: 0.875rem;
		margin: 1rem 0;
	}

	.file-input-label {
		display: inline-block;
		padding: 0.75rem 1.5rem;
		background-color: #4299e1;
		color: white;
		border-radius: 6px;
		cursor: pointer;
		font-weight: 500;
		transition: background-color 0.2s;
	}

	.file-input-label:hover {
		background-color: #3182ce;
	}

	.file-input-hidden {
		display: none;
	}

	.files-section {
		margin-top: 2rem;
	}

	.files-section h4 {
		margin: 0 0 1rem 0;
		color: #2d3748;
		font-size: 1.125rem;
	}

	.files-table {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		margin-bottom: 1.5rem;
	}

	.file-row {
		display: grid;
		grid-template-columns: 1fr auto auto auto auto;
		gap: 1rem;
		align-items: center;
		padding: 1rem;
		background-color: #f7fafc;
		border-radius: 6px;
		border: 1px solid #e2e8f0;
		transition: all 0.2s;
	}

	.file-row:hover {
		background-color: #edf2f7;
		border-color: #cbd5e0;
	}

	.file-row.complete {
		background-color: #f0fff4;
		border-color: #9ae6b4;
	}

	.file-name {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		min-width: 0;
	}

	.file-name svg {
		flex-shrink: 0;
		color: #718096;
	}

	.file-name strong {
		color: #2d3748;
		font-weight: 500;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.group-input {
		padding: 0.5rem 0.75rem;
		border: 1px solid #cbd5e0;
		border-radius: 4px;
		background-color: white;
		color: #2d3748;
		font-size: 0.875rem;
		width: 160px;
		transition: border-color 0.2s;
	}

	.group-input:hover:not(:disabled) {
		border-color: #4299e1;
	}

	.group-input:focus {
		outline: none;
		border-color: #4299e1;
		box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.1);
	}

	.group-input:disabled {
		background-color: #f7fafc;
		cursor: not-allowed;
		opacity: 0.6;
	}

	.group-input.group-missing {
		border-color: #fc8181;
	}

	.group-input.group-missing:focus {
		border-color: #e53e3e;
		box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.15);
	}

	.subscription-select {
		padding: 0.5rem 0.75rem;
		border: 1px solid #cbd5e0;
		border-radius: 4px;
		background-color: white;
		color: #2d3748;
		font-size: 0.875rem;
		cursor: pointer;
		transition: border-color 0.2s;
	}

	.subscription-select:hover:not(:disabled) {
		border-color: #4299e1;
	}

	.subscription-select:focus {
		outline: none;
		border-color: #4299e1;
		box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.1);
	}

	.subscription-select:disabled {
		background-color: #f7fafc;
		cursor: not-allowed;
		opacity: 0.6;
	}

	.status {
		padding: 0.375rem 0.75rem;
		border-radius: 4px;
		font-size: 0.875rem;
		font-weight: 500;
		white-space: nowrap;
	}

	.status-pending {
		background-color: #fef5e7;
		color: #d69e2e;
	}

	.status-uploading {
		background-color: #ebf8ff;
		color: #3182ce;
	}

	.status-complete {
		background-color: #f0fff4;
		color: #38a169;
	}

	.status-error {
		background-color: #fff5f5;
		color: #e53e3e;
	}

	.status-cancelled {
		background-color: #f7fafc;
		color: #718096;
	}

	.btn-remove {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 36px;
		height: 36px;
		border: none;
		background-color: transparent;
		color: #e53e3e;
		border-radius: 4px;
		cursor: pointer;
		transition: all 0.2s;
		padding: 0;
	}

	.btn-remove:hover:not(:disabled) {
		background-color: #fff5f5;
	}

	.btn-remove:disabled {
		color: #cbd5e0;
		cursor: not-allowed;
	}

	.actions {
		display: flex;
		justify-content: flex-end;
		padding-top: 1rem;
		border-top: 1px solid #e2e8f0;
	}

	.btn {
		padding: 0.75rem 1.5rem;
		border: none;
		border-radius: 6px;
		font-size: 0.875rem;
		font-weight: 500;
		cursor: pointer;
		transition: all 0.2s;
	}

	.btn:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.btn-primary {
		background-color: #4299e1;
		color: white;
	}

	.btn-primary:hover:not(:disabled) {
		background-color: #3182ce;
	}

	.btn-primary:active:not(:disabled) {
		transform: scale(0.98);
	}

	.btn-secondary {
		background-color: #718096;
		color: white;
	}

	.btn-secondary:hover:not(:disabled) {
		background-color: #4a5568;
	}

	.btn-secondary:active:not(:disabled) {
		transform: scale(0.98);
	}

	@media (max-width: 768px) {
		.file-row {
			grid-template-columns: 1fr auto;
			gap: 0.75rem;
		}

		.file-name {
			grid-column: 1 / -1;
		}

		.group-input {
			width: 100%;
		}

		.group-input,
		.subscription-select,
		.status {
			justify-self: start;
			grid-column: 1;
		}

		.btn-remove {
			justify-self: end;
			grid-row: 1;
			grid-column: 2;
		}
	}
</style>
