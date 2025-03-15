<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/auth';
	import { goto } from '$app/navigation';

	let { children } = $props();

	function handleLogout() {
		authStore.clearAuth();
		goto('/login');
	}

	function goHome() {
		goto('/dashboard');
	}

	const user = $derived($authStore.user);

	onMount(() => {
		const unsubscribe = authStore.subscribe((state) => {
			if (!state.isAuthenticated) {
				goto('/login');
			}
		});

		return () => {
			unsubscribe();
		};
	});
</script>

<div class="dashboard-layout">
	<nav class="navbar">
		<div class="navbar-left">
			<button class="home-button" onclick={goHome}>
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
		</div>

		<div class="navbar-right">
			{#if user}
				<span class="user-email">{user.email}</span>
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
	}

	.home-button {
		display: flex;
		align-items: center;
		background: none;
		border: none;
		color: white;
		font-size: 16px;
		cursor: pointer;
		padding: 8px 12px;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.home-button:hover {
		background-color: rgba(255, 255, 255, 0.1);
	}

	.home-button svg {
		margin-right: 8px;
	}

	.user-email {
		margin-right: 20px;
		font-size: 14px;
		background-color: rgba(255, 255, 255, 0.1);
		padding: 6px 12px;
		border-radius: 20px;
		max-width: 200px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
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

	@media (max-width: 600px) {
		.user-email {
			display: none;
		}

		.navbar {
			padding: 0 10px;
		}
	}
</style>
