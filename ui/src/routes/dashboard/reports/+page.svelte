<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import { authStore } from '$lib/stores/authStore';
	import { onMount } from 'svelte';
	import AddFileModal from './AddFileModal.svelte';
	import { getFilesList, type UserFileMetadata } from '$lib/api/fileClient';

	const isAdmin = $derived($authStore.roles?.includes('Admin'));
	let showAddFileModal = $state(false);

	let reportList = $state<UserFileMetadata[]>([]);

	function openAddFileModal() {
		showAddFileModal = true;
	}

	onMount(async () => {
		// TODO: Remove filler data once backend is ready
		const fillerData: UserFileMetadata[] = [
			{
				id: '9f4b7a27-9b38-4d6a-a6e1-4f62c5a0f33c',
				fileName: 'Monthly Report Jan 2026.pdf',
				uploadedAt: '2026-02-20T02:12:00Z'
			},
			{
				id: '8e3c8b16-8a27-5c5b-95d0-3e51b4a9e22b',
				fileName: 'Quarterly Summary Q4 2025.pdf',
				uploadedAt: '2026-02-15T14:30:00Z'
			},
			{
				id: '7d2b7a05-7916-4b4a-84d0-2d40a3a8d11a',
				fileName: 'Compliance Audit Report.pdf',
				uploadedAt: '2026-02-10T09:45:00Z'
			}
		];

		reportList = fillerData;

		const files = await getFilesList();
		if (files) {
			reportList = files;
		}
	});
</script>

<div>
	{#if isAdmin}
		<button onclick={openAddFileModal}>Open add file modal</button>
	{/if}
	
	<h2>Reports</h2>
	{#if reportList.length === 0}
		<p>No reports available</p>
	{:else}
		<ul>
			{#each reportList as file}
				<li>
					<strong>{file.fileName}</strong>
					<span>{new Date(file.uploadedAt).toLocaleDateString()}</span>
					<button>Download</button>
				</li>
			{/each}
		</ul>
	{/if}
</div>

<ModalBase bind:showModal={showAddFileModal} title="Add Files Modal" Component={AddFileModal} />
