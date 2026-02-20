<script lang="ts">
	import { uploadFile, type UploadItem } from '$lib/api/fileClient';
	import type { SubscriptionType } from '$lib/api/adminClient';
	import Page from '../+page.svelte';

	const files = $state<UploadItem[]>([]);

	const canPublish = $derived(files.some((f) => f.status === 'pending' || f.status === 'error'));
	const isUploading = $derived(files.some((f) => f.status === 'uploading'));

	function addFiles(fileList: FileList) {
		for (const file of fileList) {
			if (file.type !== 'application/pdf') {
				alert(`File ${file.name} is not a PDF and will be skipped.`);
				continue;
			}

			files.push({
				id: crypto.randomUUID(),
				file,
				requiredSubscription: 2 as SubscriptionType,
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
		for (const file of files) {
			if (file.status === 'pending' || file.status === 'error') {
				await uploadFile(file);
			}
		}
	}
</script>

<div class="dropzone" ondrop={handleDrop} ondragover={(e) => e.preventDefault()}>
	Drag PDFs here
</div>

<input type="file" multiple accept="application/pdf" onchange={handlePicker} />

{#each files as item}
	<div class="file-row">
		<strong>{item.file.name}</strong>

		<select bind:value={item.requiredSubscription} disabled={item.status === 'uploading'}>
			<option value="2">Premium</option>
		</select>

		<span>
			{#if item.status === 'pending'}
				Pending
			{/if}
			{#if item.status === 'uploading'}
				Uploading
			{/if}
			{#if item.status === 'complete'}
				Complete
			{/if}
			{#if item.status === 'error'}
				Error
			{/if}
			{#if item.status === 'cancelled'}
				Cancelled
			{/if}
		</span>

		<button onclick={() => handleRemove(item.id)} disabled={item.status === 'complete'}>
			Cancel
		</button>
	</div>

	<button onclick={publishFiles} disabled={!canPublish || isUploading}>Upload</button>
{/each}
