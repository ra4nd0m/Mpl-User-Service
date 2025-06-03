<script lang="ts">
	import { onMount } from 'svelte';
	import { deleteUser, getUsers, SubscriptionType, type UserResponse } from '$lib/api/adminClient';
	import UserRegistrationModal from './UserRegistrationModal.svelte';
	import { goto } from '$app/navigation';

	let userList: UserResponse[] = $state([]);
	let loading = $state(true);
	let error = $state<string | null>(null);
	let showUserModal = $state(false);
	let successMessage = $state<string | null>(null);

	function goToLegacy() {
		goto('/legacy/index.html#/login');
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
		successMessage = 'Пользователь успешно добавлен';
		loadUsers();
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
	<title>Admin Dashboard</title>
	<meta name="description" content="Admin dashboard for managing users and subscriptions." />
</svelte:head>

<section>
	<h1>Admin Dashboard</h1>
	<div class="actions-header">
		<div class="header-left">
			<h2>User Management</h2>
		</div>
		<div class="header-right">
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
			<button class="add-user-button" onclick={() => (showUserModal = true)}>
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
				Add User
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
			<p>Loading users...</p>
		</div>
	{:else if userList.length === 0}
		<div class="empty-state">
			<p>No users found in the system.</p>
		</div>
	{:else}
		<div class="table-container">
			<table class="users-table">
				<thead>
					<tr>
						<th>ID</th>
						<th>Email</th>
						<th>Organization</th>
						<th>INN</th>
						<th>Subscription</th>
						<th>Start Date</th>
						<th>End Date</th>
						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
					{#each userList as user}
						<tr>
							<td>{user.id}</td>
							<td>{user.email}</td>
							<td>{user.org?.name || 'N/A'}</td>
							<td>{user.org?.inn || 'N/A'}</td>
							<td>
								{#if user.org}
									<span
										class="subscription-badge {getSubscriptionTypeName(
											user.org.subscriptionType
										).toLowerCase()}"
									>
										{getSubscriptionTypeName(user.org.subscriptionType)}
									</span>
								{:else}
									N/A
								{/if}
							</td>
							<td>{formatDate(user.org?.subscriptionStartDate)}</td>
							<td>{formatDate(user.org?.subscriptionEndDate)}</td>
							<td class="actions-cell">
								<button class="action-button edit-button" title="Edit User" aria-label="Edit User">
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
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
	<UserRegistrationModal bind:showModal={showUserModal} onUserAdded={handleUserAdded} />
</section>

<style>
	.table-container {
		overflow-x: auto;
		margin-top: 1rem;
	}

	.users-table {
		width: 100%;
		border-collapse: collapse;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
		border-radius: 4px;
		overflow: hidden;
	}

	.users-table th,
	.users-table td {
		padding: 0.75rem 1rem;
		text-align: left;
		border-bottom: 1px solid #ddd;
	}

	.users-table th {
		background-color: #f8f9fa;
		font-weight: 600;
		color: #343a40;
	}

	.users-table tr:hover {
		background-color: #f1f3f5;
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

	.actions-cell {
		white-space: nowrap;
		text-align: center;
	}

	.action-button {
		background: none;
		border: none;
		cursor: pointer;
		padding: 0.25rem;
		margin: 0 0.25rem;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.action-button:hover {
		background-color: #e9ecef;
	}

	.edit-button {
		color: #228be6;
	}

	.delete-button {
		color: #fa5252;
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
</style>
