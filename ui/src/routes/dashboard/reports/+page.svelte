<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import { authStore } from '$lib/stores/authStore';
	import { onMount } from 'svelte';
	import AddFileModal from './AddFileModal.svelte';
	import {
		getFilesList,
		type UserFile,
		type DownloadStatus,
		type UserFileMetadata,
		downloadFile
	} from '$lib/api/fileClient';
	import { SubscriptionType } from '$lib/api/adminClient';

	const isAdmin = $derived($authStore.roles?.includes('Admin'));
	let showAddFileModal = $state(false);

	let reportList = $state<UserFileMetadata[]>([]);
	let reportFilesList = $derived<UserFile[]>(
		reportList.map((item) => ({
			...item,
			status: 'pending',
			abortController: null
		}))
	);

	function openAddFileModal() {
		showAddFileModal = true;
	}

	function handleDownload(file: UserFile) {
		downloadFile(file);
	}

	function handleCancelDownload(file: UserFile) {
		if (file.abortController) {
			file.abortController.abort();
		}
	}

	onMount(async () => {
		// TODO: Remove filler data once backend is ready
		const fillerData: UserFileMetadata[] = [
			{
				id: '9f4b7a27-9b38-4d6a-a6e1-4f62c5a0f33c',
				fileName: 'Monthly Report Jan 2026.pdf',
				uploadedAt: '2026-02-20T02:12:00Z',
                requiredSubscription: SubscriptionType.Premium
			},
			{
				id: '8e3c8b16-8a27-5c5b-95d0-3e51b4a9e22b',
				fileName: 'Quarterly Summary Q4 2025.pdf',
				uploadedAt: '2026-02-15T14:30:00Z',
                requiredSubscription: SubscriptionType.Premium
			},
			{
				id: '7d2b7a05-7916-4b4a-84d0-2d40a3a8d11a',
				fileName: 'Compliance Audit Report.pdf',
				uploadedAt: '2026-02-10T09:45:00Z',
                requiredSubscription: SubscriptionType.Premium
			}
		];

		reportList = fillerData;

		const files = await getFilesList();
		if (files) {
			reportList = files;
		}
	});
</script>

<svelte:head>
	<title>Reports</title>
	<meta name="description" content="View and download reports available" />
</svelte:head>

<div>
	{#if isAdmin}
		<button onclick={openAddFileModal}>Open add file modal</button>
	{/if}

	<h2>Reports</h2>
	{#if reportFilesList.length === 0}
		<p>No reports available</p>
	{:else}
		<ul>
			{#each reportFilesList as file}
				<li>
					<strong>{file.fileName}</strong>
					<span>{new Date(file.uploadedAt).toLocaleDateString()}</span>
					{#if file.status === 'pending' || file.status === 'cancelled' || file.status === 'error'}
						<button onclick={() => handleDownload(file)}>Download</button>
					{:else if file.status === 'downloading'}
						<button onclick={() => handleCancelDownload(file)}>Cancel Download</button>
					{/if}
				</li>
			{/each}
		</ul>
	{/if}
</div>

<ModalBase bind:showModal={showAddFileModal} title="Add Files Modal" Component={AddFileModal} />
