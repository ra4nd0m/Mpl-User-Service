<script lang="ts">
	import { onMount } from 'svelte';
	import { deleteUser, getUsers, SubscriptionType, type UserResponse } from '$lib/api/adminClient';
	import UserRegistrationModal from './UserRegistrationModal.svelte';
	import { goto } from '$app/navigation';

	import { m } from '$lib/i18n';
	import ModalBase from '$components/ModalBase/ModalBase.svelte';

	let userList: UserResponse[] = $state([]);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let showUserModal = $state(false);
	let successMessage = $state<string | null>(null);

	let modalMode: 'create' | 'edit' = $state('create');
	let selectedUser: UserResponse | null = $state(null);
	let isRefreshPending = $state(false);

	function goToLegacy() {
		goto('/legacy/index.html#/login');
	}

	function goToFilters() {
		goto('/dashboard/admin/filters');
	}

	function getSubscriptionTypeName(type: SubscriptionType | undefined): string {
		if (type === undefined) return 'N/A';

		switch (type) {
			case SubscriptionType.Free:
				return 'Free';
			case SubscriptionType.Basic:
				return 'Basic';
			case SubscriptionType.Premium:
				return 'Premium';
			default:
				return 'N/A';
		}
	}

	function formatDate(dateString: string | undefined): string {
		if (dateString === undefined) return 'N/A';
		return new Date(dateString).toLocaleDateString();
	}

	function handleUserAdded() {
		showUserModal = false;
		successMessage =
			modalMode === 'edit' ? 'Пользователь успешно обновлен' : 'Пользователь успешно добавлен';

		modalMode = 'create';
		selectedUser = null;
	}

	$effect(() => {
		if (!showUserModal && isRefreshPending) {
			loadUsers();
			isRefreshPending = false;
		}
	});

	function handleEditUser(user: UserResponse) {
		selectedUser = user;
		modalMode = 'edit';
		showUserModal = true;
	}

	function handleAddUser() {
		modalMode = 'create';
		selectedUser = null;
		showUserModal = true;
	}

	async function loadUsers() {
		try {
			loading = true;
			const users = await getUsers();
			userList = users ?? [];
		} catch (err) {
			console.error('Failed to fetch users', err);
			error = 'Failed to fetch users';
		} finally {
			loading = false;
		}
	}

	async function handleDeleteUser(email: string) {
		if (!confirm(`Вы уверены, что хотите удалить пользователя ${email}?`)) return;

		try {
			loading = true;
			error = null;
			successMessage = null;

			const result = await deleteUser(email);

			if (result) {
				successMessage = `Пользователь ${email} успешно удален`;
				await loadUsers();
			} else {
				error = `Не удалось удалить пользователя ${email}`;
			}
		} catch (err) {
			console.error('Failed to delete user', err);
			error = 'Failed to delete user';
		} finally {
			loading = false;
		}
	}

	onMount(async () => {
		await loadUsers();
	});
</script>

<svelte:head>
	<title>{m.admin_dashboard_header()}</title>
	<meta name="description" content="Admin dashboard for managing users and subscriptions." />
</svelte:head>

<section>
	<h1>{m.admin_dashboard_header()}</h1>
	<div class="actions-header">
		<div class="header-left">
			<h2>{m.admin_user_management()}</h2>
		</div>
		<div class="header-right">
			<button class="filters-button" onclick={goToFilters}>
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
					<polygon points="22,3 2,3 10,12.46 10,19 14,21 14,12.46"></polygon>
				</svg>
				{m.admin_filters_button()}
			</button>
			<button class="legacy-button" onclick={goToLegacy}>
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
					<path d="M12 19l9 2-9-18-9 18 9-2z"></path>
				</svg>
				Legacy System
			</button>
			<button class="add-user-button" onclick={handleAddUser}>
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
					<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"></path>
					<circle cx="9" cy="7" r="4"></circle>
					<line x1="19" y1="8" x2="19" y2="14"></line>
					<line x1="16" y1="11" x2="22" y2="11"></line>
				</svg>
				{m.admin_add_user_button()}
			</button>
		</div>
	</div>

	{#if error}
		<div class="error-message">{error}</div>
	{/if}
	{#if successMessage}
		<div class="success-message">{successMessage}</div>
	{/if}
	{#if loading}
		<div class="loading-spinner-container">
			<div class="loading-spinner"></div>
			<p>{m.admin_loading_users()}</p>
		</div>
	{:else if userList.length === 0}
		<div class="empty-state">
			<p>{m.admin_no_users()}</p>
		</div>
	{:else}
		<div class="table-container">
			<div class="table-wrapper">
				<table class="users-table">
					<thead>
						<tr>
							<th class="email-col">{m.admin_user_table_email()}</th>
							<th class="org-col">{m.admin_user_table_organization()}</th>
							<th class="inn-col">{m.admin_user_table_inn()}</th>
							<th class="export-col">{m.admin_user_table_can_export_data()}</th>
							<th class="sub-col">{m.admin_user_table_subscription()}</th>
							<th class="date-col">{m.admin_user_table_start_date()}</th>
							<th class="date-col">{m.admin_user_table_end_date()}</th>
							<th class="actions-col">{m.admin_user_table_actions()}</th>
						</tr>
					</thead>
					<tbody>
						{#each userList as user (user.email)}
							<tr>
								<td class="email-col">{user.email}</td>
								<td class="org-col">
									{#if user.org}
										<span>{user.org.name}</span>
									{:else if user.sub}
										<span>Individual</span>
									{:else}
										<span>None</span>
									{/if}
								</td>
								<td class="inn-col">{user.org?.inn || 'N/A'}</td>
								<td class="export-col">
									<span class={user.canExportData ? 'export-allowed' : 'export-denied'}>
										{user.canExportData ? m.universal_yes() : m.universal_no()}
									</span>
								</td>
								<td class="sub-col">
									{#if user.org}
										<span
											class="subscription-badge {getSubscriptionTypeName(
												user.org.subscriptionType
											).toLowerCase()}"
										>
											{getSubscriptionTypeName(user.org.subscriptionType)}
										</span>
									{:else if user.sub}
										<span
											class="subscription-badge {getSubscriptionTypeName(
												user.sub.subscriptionType
											).toLowerCase()}"
										>
											{getSubscriptionTypeName(user.sub.subscriptionType)}
										</span>
									{:else}
										N/A
									{/if}
								</td>
								<td class="date-col"
									>{#if user.org}
										{formatDate(user.org?.subscriptionStartDate)}
									{:else if user.sub}
										{formatDate(user.sub?.subscriptionStartDate)}
									{/if}</td
								>
								<td class="date-col">
									{#if user.org}
										{formatDate(user.org?.subscriptionEndDate)}
									{:else if user.sub}
										{formatDate(user.sub?.subscriptionEndDate)}
									{/if}
								</td>
								<td class="actions-col">
									<div class="table-actions">
										<button
											class="action-button edit-button"
											title="Edit User"
											aria-label="Edit User"
											onclick={() => handleEditUser(user)}
										>
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
												<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
												<path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
											</svg>
										</button>
										<button
											class="action-button delete-button"
											title="Delete User"
											aria-label="Delete User"
											onclick={() => handleDeleteUser(user.email)}
										>
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
												<path d="M3 6h18"></path>
												<path
													d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"
												></path>
											</svg>
										</button>
									</div>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</div>
	{/if}
	<ModalBase
		bind:showModal={showUserModal}
		title={modalMode === 'edit' ? 'Редактировать пользователя' : m.admin_create_user_header()}
		Component={UserRegistrationModal}
		componentProps={{
			onUserAdded: handleUserAdded,
			mode: modalMode,
			existingUser: selectedUser,
			requestUsersRefresh: () => {
				isRefreshPending = true;
			}
		}}
	/>
</section>

<style>
	/* Table Styles */
	.table-container {
		margin-top: 1rem;
		border: 1px solid #e9ecef;
		border-radius: 8px;
		overflow: hidden;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	}

	.table-wrapper {
		overflow-x: auto;
		-webkit-overflow-scrolling: touch;
	}

	.users-table {
		width: 100%;
		min-width: 900px;
		border-collapse: collapse;
		background-color: white;
	}

	.users-table th,
	.users-table td {
		padding: 0.75rem;
		text-align: left;
		border-bottom: 1px solid #e9ecef;
		vertical-align: middle;
		overflow-wrap: anywhere;
	}

	.users-table th {
		background-color: #f8f9fa;
		font-weight: 600;
		color: #495057;
		position: sticky;
		top: 0;
		z-index: 1;
	}

	.users-table tbody tr {
		transition: background-color 0.2s ease;
	}

	.users-table tbody tr:hover {
		background-color: #f1f3f5;
	}

	.users-table tbody tr:last-child td {
		border-bottom: none;
	}

	/* Column sizing */
	.users-table th.email-col,
	.users-table td.email-col {
		min-width: 200px;
		max-width: 250px;
		white-space: normal;
		overflow-wrap: anywhere;
	}

	.users-table th.org-col,
	.users-table td.org-col {
		min-width: 150px;
		max-width: 200px;
		white-space: normal;
		overflow-wrap: anywhere;
	}

	.inn-col {
		min-width: 120px;
	}

	.export-col {
		min-width: 80px;
		text-align: center;
	}

	.sub-col {
		min-width: 100px;
		text-align: center;
	}

	.date-col {
		min-width: 110px;
	}

	.actions-col {
		width: 120px;
		text-align: center;
	}

	.table-actions {
		display: flex;
		gap: 0.5rem;
		justify-content: center;
		align-items: center;
	}

	.export-allowed {
		color: #28a745;
		font-weight: 600;
	}

	.export-denied {
		color: #dc3545;
		font-weight: 600;
	}

	.subscription-badge {
		display: inline-block;
		padding: 0.25rem 0.5rem;
		border-radius: 4px;
		font-size: 0.8rem;
		font-weight: 600;
		text-transform: uppercase;
	}

	.subscription-badge.free {
		background-color: #e9ecef;
		color: #495057;
	}

	.subscription-badge.basic {
		background-color: #4dabf7;
		color: white;
	}

	.subscription-badge.premium {
		background-color: #f783ac;
		color: white;
	}

	.action-button {
		background: none;
		border: none;
		cursor: pointer;
		padding: 0.375rem;
		border-radius: 6px;
		transition: all 0.2s ease;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.action-button:hover {
		background-color: rgba(0, 0, 0, 0.1);
		transform: scale(1.05);
	}

	.edit-button {
		color: #1976d2;
	}

	.edit-button:hover {
		background-color: rgba(25, 118, 210, 0.1);
	}

	.delete-button {
		color: #d32f2f;
	}

	.delete-button:hover {
		background-color: rgba(211, 47, 47, 0.1);
	}

	.loading-spinner-container {
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 2rem;
	}

	.loading-spinner {
		width: 40px;
		height: 40px;
		border: 4px solid #f3f3f3;
		border-top: 4px solid #3498db;
		border-radius: 50%;
		animation: spin 1s linear infinite;
		margin-bottom: 1rem;
	}

	@keyframes spin {
		0% {
			transform: rotate(0deg);
		}
		100% {
			transform: rotate(360deg);
		}
	}

	.error-message {
		padding: 0.75rem;
		background-color: rgba(220, 53, 69, 0.1);
		color: #dc3545;
		border-radius: 4px;
		margin-bottom: 1rem;
	}

	.empty-state {
		text-align: center;
		padding: 2rem;
		background-color: #f8f9fa;
		border-radius: 4px;
		color: #6c757d;
	}
	.actions-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1rem;
	}

	.add-user-button {
		display: flex;
		align-items: center;
		background-color: #2ecc71;
		color: white;
		border: none;
		padding: 0.5rem 1rem;
		border-radius: 4px;
		font-weight: 600;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.add-user-button svg {
		margin-right: 8px;
	}

	.add-user-button:hover {
		background-color: #27ae60;
	}

	.success-message {
		padding: 0.75rem;
		background-color: rgba(40, 167, 69, 0.1);
		color: #28a745;
		border-radius: 4px;
		margin-bottom: 1rem;
	}
	.actions-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1rem;
	}

	.header-right {
		display: flex;
		gap: 0.75rem;
	}

	.legacy-button {
		display: flex;
		align-items: center;
		background-color: #3498db;
		color: white;
		border: none;
		padding: 0.5rem 1rem;
		border-radius: 4px;
		font-weight: 600;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.legacy-button svg {
		margin-right: 8px;
	}

	.legacy-button:hover {
		background-color: #2980b9;
	}
	.filters-button {
		display: flex;
		align-items: center;
		background-color: #9b59b6;
		color: white;
		border: none;
		padding: 0.5rem 1rem;
		border-radius: 4px;
		font-weight: 600;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.filters-button svg {
		margin-right: 8px;
	}

	/* Mobile Responsive Styles */
	@media (max-width: 768px) {
		.actions-header {
			flex-direction: column;
			align-items: stretch;
			gap: 1rem;
		}

		.header-right {
			display: flex;
			flex-direction: column;
			gap: 0.75rem;
		}

		.header-left h2 {
			margin: 0;
			font-size: 1.3rem;
		}

		.add-user-button,
		.legacy-button,
		.filters-button {
			width: 100%;
			justify-content: center;
			padding: 0.75rem 1rem;
			font-size: 1rem;
		}

		.users-table {
			min-width: 800px;
			font-size: 0.9rem;
		}

		.users-table th,
		.users-table td {
			padding: 0.625rem 0.5rem;
		}

		.users-table th.email-col,
		.users-table td.email-col {
			min-width: 180px;
			max-width: 200px;
		}

		.users-table th.org-col,
		.users-table td.org-col {
			min-width: 120px;
			max-width: 150px;
		}

		.inn-col {
			min-width: 100px;
		}

		.actions-col {
			width: 100px;
		}

		.action-button {
			padding: 0.25rem;
		}

		.action-button svg {
			width: 14px;
			height: 14px;
		}
	}

	@media (max-width: 480px) {
		.users-table {
			min-width: 750px;
			font-size: 0.85rem;
		}

		.users-table th,
		.users-table td {
			padding: 0.5rem 0.375rem;
		}

		.users-table th.email-col,
		.users-table td.email-col {
			min-width: 160px;
			max-width: 180px;
		}

		.users-table th.org-col,
		.users-table td.org-col {
			min-width: 100px;
			max-width: 120px;
		}

		.subscription-badge {
			font-size: 0.7rem;
			padding: 0.2rem 0.4rem;
		}
	}
</style>
