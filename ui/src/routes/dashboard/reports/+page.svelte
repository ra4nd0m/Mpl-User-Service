<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import ConfirmationDialog from '$components/ConfirmationDialog/ConfirmationDialog.svelte';
	import { authStore } from '$lib/stores/authStore';
	import { onMount } from 'svelte';
	import AddFileModal from './AddFileModal.svelte';
	import {
		getFilesList,
		type UserFile,
		type DownloadStatus,
		type UserFileMetadata,
		downloadFile,
		deleteFile
	} from '$lib/api/fileClient';
	import { SubscriptionType } from '$lib/api/adminClient';

	const isAdmin = $derived($authStore.roles?.includes('Admin'));
	const userSubscriptionLevel = $derived(
		SubscriptionType[$authStore.user?.subscriptionType as keyof typeof SubscriptionType] ??
			SubscriptionType.Free
	);

	function canDownload(file: UserFile): boolean {
		return isAdmin || userSubscriptionLevel >= file.requiredSubscription;
	}

	let showAddFileModal = $state(false);
	let showDeleteConfirmation = $state(false);
	let fileToDelete = $state<string | null>(null);

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

	function handleDelete(fileId: string) {
		fileToDelete = fileId;
		showDeleteConfirmation = true;
	}

	function confirmDelete() {
		if (!fileToDelete) return;

		deleteFile(fileToDelete)
			.then(() => {
				reportList = reportList.filter((f) => f.id !== fileToDelete);
				showDeleteConfirmation = false;
				fileToDelete = null;
			})
			.catch((err) => {
				console.error(`Failed to delete file: ${err}`);
				alert('Failed to delete file. Please try again.');
			});
	}

	function cancelDelete() {
		showDeleteConfirmation = false;
		fileToDelete = null;
	}

	onMount(async () => {
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
						{#if canDownload(file)}
							<button onclick={() => handleDownload(file)}>Download</button>
						{/if}
					{:else if file.status === 'downloading'}
						<button onclick={() => handleCancelDownload(file)}>Cancel Download</button>
					{/if}
					{#if isAdmin}
						<button onclick={() => handleDelete(file.id)}>Delete</button>
					{/if}
				</li>
			{/each}
		</ul>
	{/if}
</div>

<ModalBase bind:showModal={showAddFileModal} title="Add Files Modal" Component={AddFileModal} />
<ModalBase
	bind:showModal={showDeleteConfirmation}
	title="Confirm Deletion"
	size={{ width: '500px', height: 'auto' }}
	Component={ConfirmationDialog}
	componentProps={{
		message: 'Are you sure you want to delete this file? This action cannot be undone.',
		confirmText: 'Delete',
		onConfirm: confirmDelete,
		onCancel: cancelDelete
	}}
/>
