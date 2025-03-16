<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import { goto } from '$app/navigation';
	import { logout } from '$lib/api/authClient';
	import { browser } from '$app/environment';

	let { children } = $props();

	async function handleLogout() {
		await logout();
		goto('/login');
	}

	function goHome() {
		goto('/dashboard');
	}

	function goToMaterials() {
		goto('/dashboard/materials');
	}

	function goToAdmin() {
		goto('/dashboard/admin');
	}

	const user = $derived($authStore.user);
	const isAdmin = $derived($authStore.roles?.includes('Admin'));

	onMount(() => {
		const unsubscribe = authStore.subscribe((state) => {
			if (!state.isAuthenticated && browser) {
				goto('/login');
			}
		});
		return unsubscribe;
	});
</script>

<div class="dashboard-layout">
	<nav class="navbar">
		<div class="navbar-left">
			<button class="nav-button" onclick={goHome}>
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="20"
					height="20"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
					<polyline points="9 22 9 12 15 12 15 22"></polyline>
				</svg>
				Home
			</button>

			<button class="nav-button" onclick={goToMaterials}>
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="20"
					height="20"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"></path>
				</svg>
				All Materials
			</button>

			{#if isAdmin}
				<button class="nav-button admin-button" onclick={goToAdmin}>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						width="20"
						height="20"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
					>
						<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"></path>
						<circle cx="9" cy="7" r="4"></circle>
						<path d="M22 21v-2a4 4 0 0 0-3-3.87"></path>
						<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
					</svg>
					Admin Panel
				</button>
			{/if}
		</div>

		<div class="navbar-right">
			{#if user}
				<span class="user-email">{user.email}</span>
				{#if user.subscriptionType}
					<span class="subscription-badge">{user.subscriptionType}</span>
				{/if}
			{/if}
			<button class="logout-button" onclick={handleLogout}>
				Logout
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
					<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
					<polyline points="16 17 21 12 16 7"></polyline>
					<line x1="21" y1="12" x2="9" y2="12"></line>
				</svg>
			</button>
		</div>
	</nav>

	<main class="content">
		{@render children()}
	</main>
</div>

<style>
	.dashboard-layout {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
	}

	.navbar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background-color: #2c3e50;
		color: white;
		padding: 0 20px;
		height: 60px;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	}

	.navbar-left,
	.navbar-right {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	.nav-button {
		display: flex;
		align-items: center;
		background: none;
		border: none;
		color: white;
		font-size: 14px;
		cursor: pointer;
		padding: 8px 12px;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.nav-button:hover {
		background-color: rgba(255, 255, 255, 0.1);
	}

	.nav-button svg {
		margin-right: 8px;
	}

	.admin-button {
		background-color: rgba(231, 76, 60, 0.2);
	}

	.admin-button:hover {
		background-color: rgba(231, 76, 60, 0.3);
	}

	.user-email {
		margin-right: 8px;
		font-size: 14px;
		background-color: rgba(255, 255, 255, 0.1);
		padding: 6px 12px;
		border-radius: 20px;
		max-width: 200px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.subscription-badge {
		font-size: 12px;
		padding: 4px 8px;
		border-radius: 12px;
		background-color: #3498db;
		margin-right: 12px;
	}

	.logout-button {
		display: flex;
		align-items: center;
		background-color: rgba(255, 255, 255, 0.1);
		border: none;
		color: white;
		padding: 8px 16px;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.logout-button:hover {
		background-color: rgba(255, 255, 255, 0.2);
	}

	.logout-button svg {
		margin-left: 8px;
	}

	.content {
		flex: 1;
		padding: 20px;
		background-color: #f5f5f5;
	}

	@media (max-width: 768px) {
		.navbar-left {
			overflow-x: auto;
			-webkit-overflow-scrolling: touch;
			scrollbar-width: none; /* Firefox */
		}

		.navbar-left::-webkit-scrollbar {
			display: none; /* Chrome, Safari, Edge */
		}
	}

	@media (max-width: 600px) {
		.user-email {
			display: none;
		}

		.subscription-badge {
			margin-right: 8px;
		}

		.navbar {
			padding: 0 10px;
		}

		.nav-button {
			padding: 8px 6px;
			font-size: 12px;
		}
	}
</style>
