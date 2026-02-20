<script lang="ts">
	import { uploadFile, type UploadItem } from '$lib/api/fileClient';
	import { SubscriptionType } from '$lib/api/adminClient';

	const subscriptionOptions = (Object.entries(SubscriptionType) as [string, number][]).filter(
		([, v]) => typeof v === 'number'
	);

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
	}
</script>

<div
	class="dropzone"
	role="button"
	tabindex="0"
	ondrop={handleDrop}
	ondragover={(e) => e.preventDefault()}
>
	Drag PDFs here
</div>

<input type="file" multiple accept="application/pdf" onchange={handlePicker} />

{#each files as item}
	<div class="file-row">
		<strong>{item.file.name}</strong>

		<select bind:value={item.requiredSubscription} disabled={item.status === 'uploading'}>
			{#each subscriptionOptions as [label, value]}
				<option {value}>{label}</option>
			{/each}
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
