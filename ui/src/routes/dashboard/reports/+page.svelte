<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import ConfirmationDialog from '$components/ConfirmationDialog/ConfirmationDialog.svelte';
	import GroupSelector from '$components/GroupSelector/GroupSelector.svelte';
	import { authStore } from '$lib/stores/authStore';
	import { onMount } from 'svelte';
	import AddFileModal from './AddFileModal.svelte';
	import { getFilesList, type UserFile, downloadFile, deleteFile } from '$lib/api/fileClient';
	import { SubscriptionType } from '$lib/api/adminClient';
	import { m } from '$lib/i18n';

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

	let fileMap = $state<UserFile[]>([]);
	const existingGroups = $derived<string[]>([
		...new Set(fileMap.map((f) => f.group).filter(Boolean))
	]);
	const groupOptions = $derived(existingGroups.map((g) => ({ value: g, label: g })));
	let selectedGroup = $state<string>('');
	let sortDir = $state<'asc' | 'desc'>('desc');
	const reportFilesList = $derived(
		fileMap
			.filter((f) => selectedGroup === '' || f.group === selectedGroup)
			.toSorted((a, b) => {
				const diff = new Date(a.uploadedAt).getTime() - new Date(b.uploadedAt).getTime();
				return sortDir === 'desc' ? -diff : diff;
			})
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
				fileMap = fileMap.filter((f) => f.id !== fileToDelete);
				showDeleteConfirmation = false;
				fileToDelete = null;
			})
			.catch((err) => {
				console.error(`Failed to delete file: ${err}`);
				alert(m.reports_delete_failed());
			});
	}

	function cancelDelete() {
		showDeleteConfirmation = false;
		fileToDelete = null;
	}

	async function refreshReportList() {
		const files = await getFilesList();
		if (files) {
			fileMap = files.map((f) => ({ ...f, status: 'pending', abortController: null }));
		}
	}

	onMount(async () => {
		await refreshReportList();
	});
</script>

<svelte:head>
	<title>{m.reports_page_title()}</title>
	<meta name="description" content={m.reports_page_description()} />
</svelte:head>

<div class="reports-page">
	<div class="header">
		<h2>{m.reports_header()}</h2>
		{#if isAdmin}
			<button class="btn btn-primary" onclick={openAddFileModal}>
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="18"
					height="18"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				{m.reports_add_files_button()}
			</button>
		{/if}
	</div>

	{#if existingGroups.length > 0}
		<GroupSelector
			bind:selected={selectedGroup}
			groups={groupOptions}
			label={m.reports_filter_by_group()}
			allLabel={m.reports_all_groups()}
		/>
	{/if}

	{#if fileMap.length === 0}
		<div class="empty-state">
			<svg
				xmlns="http://www.w3.org/2000/svg"
				width="64"
				height="64"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				stroke-linecap="round"
				stroke-linejoin="round"
			>
				<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
				<polyline points="14 2 14 8 20 8"></polyline>
			</svg>
			<p>{m.reports_no_reports_available()}</p>
		</div>
	{:else if reportFilesList.length === 0}
		<div class="empty-state">
			<svg
				xmlns="http://www.w3.org/2000/svg"
				width="64"
				height="64"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="2"
				stroke-linecap="round"
				stroke-linejoin="round"
			>
				<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
				<polyline points="14 2 14 8 20 8"></polyline>
			</svg>
			<p>{m.reports_no_reports_in_group({ group: selectedGroup })}</p>
		</div>
	{:else}
		<div class="reports-table">
			<div class="table-header">
				<div class="col-filename">{m.reports_table_file_name()}</div>
				<div class="col-group">{m.reports_table_group()}</div>
				<div class="col-date">
					<button class="sort-btn" onclick={() => (sortDir = sortDir === 'desc' ? 'asc' : 'desc')}>
						{m.reports_table_upload_date()}
						<span class="sort-arrow">{sortDir === 'desc' ? '↓' : '↑'}</span>
					</button>
				</div>
				<div class="col-subscription">{m.reports_table_required()}</div>
				<div class="col-actions">{m.reports_table_actions()}</div>
			</div>
			{#each reportFilesList as file}
				<div class="table-row">
					<div class="col-filename">
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
						<span class="filename">{file.fileName}</span>
					</div>
					<div class="col-group">
						<span class="group-tag">{file.group}</span>
					</div>
					<div class="col-date">
						{new Date(file.uploadedAt).toLocaleDateString()}
					</div>
					<div class="col-subscription">
						<span class="badge badge-{file.requiredSubscription}">
							{file.requiredSubscription === SubscriptionType.Free
								? m.reports_subscription_free()
								: file.requiredSubscription === SubscriptionType.Basic
									? m.reports_subscription_basic()
									: m.reports_subscription_premium()}
						</span>
					</div>
					<div class="col-actions">
						{#if file.status === 'pending' || file.status === 'cancelled' || file.status === 'error'}
							{#if canDownload(file)}
								<button class="btn btn-sm btn-primary" onclick={() => handleDownload(file)}>
									<svg
										xmlns="http://www.w3.org/2000/svg"
										width="16"
										height="16"
										viewBox="0 0 24 24"
										fill="none"
										stroke="currentColor"
										stroke-width="2"
										stroke-linecap="round"
										stroke-linejoin="round"
									>
										<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
										<polyline points="7 10 12 15 17 10"></polyline>
										<line x1="12" y1="15" x2="12" y2="3"></line>
									</svg>
									{m.reports_download_button()}
								</button>
							{:else}
								<span class="access-denied">{m.reports_requires_higher_subscription()}</span>
							{/if}
						{:else if file.status === 'downloading'}
							<button class="btn btn-sm btn-secondary" onclick={() => handleCancelDownload(file)}>
								{m.reports_cancel_button()}
							</button>
						{/if}
						{#if isAdmin}
							<button class="btn btn-sm btn-danger" onclick={() => handleDelete(file.id)}>
								<svg
									xmlns="http://www.w3.org/2000/svg"
									width="16"
									height="16"
									viewBox="0 0 24 24"
									fill="none"
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
								>
									<polyline points="3 6 5 6 21 6"></polyline>
									<path
										d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
									></path>
								</svg>
								{m.reports_delete_button()}
							</button>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<ModalBase
	bind:showModal={showAddFileModal}
	title={m.reports_add_files_modal_title()}
	Component={AddFileModal}
	componentProps={{ refreshReports: refreshReportList, existingGroups }}
/>
<ModalBase
	bind:showModal={showDeleteConfirmation}
	title={m.reports_confirm_deletion_title()}
	size={{ width: '500px', height: 'auto' }}
	Component={ConfirmationDialog}
	componentProps={{
		message: m.reports_confirm_deletion_message(),
		confirmText: m.reports_delete_confirm_button(),
		onConfirm: confirmDelete,
		onCancel: cancelDelete
	}}
/>

<style>
	.reports-page {
		padding: 2rem;
		max-width: 1400px;
		margin: 0 auto;
	}

	.header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.header h2 {
		margin: 0;
		color: #2d3748;
		font-size: 1.875rem;
		font-weight: 700;
	}

	.btn {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
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

	.btn-secondary {
		background-color: #718096;
		color: white;
	}

	.btn-secondary:hover:not(:disabled) {
		background-color: #4a5568;
	}

	.btn-danger {
		background-color: #e53e3e;
		color: white;
	}

	.btn-danger:hover:not(:disabled) {
		background-color: #c53030;
	}

	.btn:active:not(:disabled) {
		transform: scale(0.98);
	}

	.btn-sm {
		padding: 0.5rem 1rem;
		font-size: 0.8125rem;
	}

	.btn svg {
		flex-shrink: 0;
	}

	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 4rem 2rem;
		background-color: #f7fafc;
		border-radius: 8px;
		border: 2px dashed #cbd5e0;
	}

	.empty-state svg {
		color: #cbd5e0;
		margin-bottom: 1rem;
	}

	.empty-state p {
		color: #718096;
		font-size: 1.125rem;
		margin: 0;
	}

	.reports-table {
		background-color: white;
		border-radius: 8px;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
		overflow: hidden;
	}

	.table-header {
		display: grid;
		grid-template-columns: 2fr 1fr 1fr 1fr 2fr;
		gap: 1rem;
		padding: 1rem 1.5rem;
		background-color: #f7fafc;
		border-bottom: 1px solid #e2e8f0;
		font-weight: 600;
		font-size: 0.875rem;
		color: #4a5568;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.table-row {
		display: grid;
		grid-template-columns: 2fr 1fr 1fr 1fr 2fr;
		gap: 1rem;
		padding: 1.25rem 1.5rem;
		border-bottom: 1px solid #e2e8f0;
		transition: background-color 0.2s;
		align-items: center;
	}

	.table-row:hover {
		background-color: #f7fafc;
	}

	.table-row:last-child {
		border-bottom: none;
	}

	.col-filename {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		min-width: 0;
	}

	.col-filename svg {
		flex-shrink: 0;
		color: #4299e1;
	}

	.filename {
		color: #2d3748;
		font-weight: 500;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.col-group {
		display: flex;
		align-items: center;
	}

	.group-tag {
		display: inline-block;
		padding: 0.25rem 0.625rem;
		background-color: #edf2f7;
		color: #4a5568;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 500;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 140px;
	}

	.col-date {
		color: #718096;
		font-size: 0.875rem;
	}

	.sort-btn {
		display: inline-flex;
		align-items: center;
		gap: 0.3rem;
		background: none;
		border: none;
		padding: 0;
		font: inherit;
		font-weight: 600;
		color: #4a5568;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		cursor: pointer;
		transition: color 0.15s;
	}

	.sort-btn:hover {
		color: #2d3748;
	}

	.sort-arrow {
		font-size: 0.75rem;
	}

	.col-subscription {
		display: flex;
		align-items: center;
	}

	.badge {
		display: inline-block;
		padding: 0.25rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.025em;
	}

	.badge-0 {
		background-color: #e6fffa;
		color: #234e52;
	}

	.badge-1 {
		background-color: #fef5e7;
		color: #744210;
	}

	.badge-2 {
		background-color: #ede9fe;
		color: #5b21b6;
	}

	.col-actions {
		display: flex;
		gap: 0.5rem;
		justify-content: flex-end;
	}

	.access-denied {
		color: #e53e3e;
		font-size: 0.8125rem;
		font-style: italic;
	}

	@media (max-width: 1024px) {
		.table-header,
		.table-row {
			grid-template-columns: 2fr 1fr 1fr 1.5fr;
		}

		.col-subscription {
			display: none;
		}

		.table-header .col-subscription {
			display: none;
		}
	}

	@media (max-width: 768px) {
		.reports-page {
			padding: 1rem;
		}

		.header {
			flex-direction: column;
			align-items: flex-start;
			gap: 1rem;
		}

		.table-header {
			display: none;
		}

		.table-row {
			grid-template-columns: 1fr;
			gap: 0.75rem;
			padding: 1rem;
		}

		.col-group,
		.col-date,
		.col-subscription {
			display: block;
			font-size: 0.8125rem;
		}

		.col-actions {
			flex-wrap: wrap;
			justify-content: flex-start;
		}
	}
</style>
