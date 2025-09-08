<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import { goto } from '$app/navigation';
	import { logout, refreshAccessToken } from '$lib/api/authClient';
	import { browser } from '$app/environment';
	import SubscriptionInfo from './components/SubscriptionInfo.svelte';
	import { locales, switchLocale, locale, m } from '$lib/i18n';

	let { children } = $props();
	let checkingAuth = $state(true);

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

	function handleLanguageChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		switchLocale(target.value as 'en' | 'ru');
	}

	const user = $derived($authStore.user);
	const isAdmin = $derived($authStore.roles?.includes('Admin'));

	onMount(async () => {
		let isAuthenticated = false;
		let token: string | null = null;

		// Check initial state from store
		const unsubscribeInitial = authStore.subscribe((state) => {
			isAuthenticated = state.isAuthenticated;
			token = state.token;
		});
		unsubscribeInitial(); // Unsubscribe immediately

		if (isAuthenticated && token) {
			// Already authenticated, load data if not already loaded/loading
			if (!$favoritesStore.loading && $favoritesStore.ids.length === 0) {
				await favoritesStore.loadFavourites();
			}
			checkingAuth = false;
		} else {
			// Not authenticated or no token, try refreshing
			const newToken = await refreshAccessToken();
			if (newToken) {
				authStore.setToken(newToken);
				// Now authenticated, load data
				await favoritesStore.loadFavourites();
				checkingAuth = false;
			} else {
				// Refresh failed, redirect to login
				if (browser) {
					goto('/login');
				}
				// Keep checkingAuth true as we are navigating away
			}
		}
	});
</script>

{#key $locale}
	<div class="dashboard-layout">
		<nav class="navbar">
			<div class="navbar-left">
				<div class="logo">
					<svg
						width="235"
						height="25"
						viewBox="0 0 235 25"
						fill="none"
						xmlns="http://www.w3.org/2000/svg"
					>
						<path
							d="M0 0.489445V25H5.32212V7.5715L10.1066 25H14.8374L19.4069 7.51617V25H26.2343V0.378788H16.5577L12.7946 12.6617L9.40779 0.378788L0 0.489445Z"
							fill="#757575"
						></path>
						<path
							d="M30.6682 0.378788V25H48.4041V18.9692H37.9214V14.9855H46.1276V9.78464H37.8155V5.74566H48.4041V0.378788H30.6682Z"
							fill="#757575"
						></path>
						<path
							d="M217.264 0.378788V25H235V18.9692H224.517V14.9855H232.723V9.78464H224.411V5.74566H235V0.378788H217.264Z"
							fill="#EA5B21"
						></path>
						<path
							d="M49.5126 0.378788V5.85632H54.278V25H61.6381V5.85632H66.5094V0.378788H49.5126Z"
							fill="#757575"
						></path>
						<path
							d="M80.165 0.378788H72.3299L65.0314 25H70.9883L72.3299 19.9651H78.7697L80.2187 25H87.5708L80.165 0.378788ZM73.4032 14.9302L75.4961 7.62683L77.7501 14.9302H73.4032Z"
							fill="#757575"
						></path>
						<path
							d="M185.102 0.378788H177.267L169.969 25H175.925L177.267 19.9651H183.707L185.156 25H192.508L185.102 0.378788ZM178.394 14.9302L180.487 7.62683L182.741 14.9302H178.394Z"
							fill="#EA5B21"
						></path>
						<path d="M89.7877 0.378788V25H106.415V18.9692H97.0521V0.378788H89.7877Z" fill="#757575"
						></path>
						<path d="M109.371 0.378788V25H125.629V18.9692H116.474V0.378788H109.371Z" fill="#757575"
						></path>
						<path d="M151.494 0.378788V25H167.752V18.9692H158.567V0.378788H151.494Z" fill="#EA5B21"
						></path>
						<path
							d="M141.591 0.434118L128.585 0.378788V25H135.738V15.8708H142.458C142.458 15.8708 148.907 14.7089 148.907 7.90347C148.961 1.15339 141.591 0.434118 141.591 0.434118ZM139.64 10.6146H135.792V5.52435H139.423C139.423 5.52435 141.537 5.74566 141.537 8.06946C141.537 10.3933 139.64 10.6146 139.64 10.6146Z"
							fill="#EA5B21"
						></path>
						<path
							d="M207.999 9.66939H214.292C214.292 9.66939 214.345 -0.162736 204.668 0.00204841C194.991 0.166833 193.616 9.61446 193.616 12.251C193.616 14.8876 194.092 24.7197 204.615 24.9943C215.138 25.269 214.292 15.4918 214.292 15.4918H207.999C207.999 15.4918 207.365 19.7212 204.562 19.7212C199.592 19.5015 199.327 5.385 204.827 5.385C208.211 5.385 207.999 9.66939 207.999 9.66939Z"
							fill="#EA5B21"
						></path>
					</svg>
				</div>
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
					{m.nav_home()}
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
					{m.nav_all_materials()}
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
						{m.nav_admin_panel()}
					</button>
				{/if}
			</div>

			<div class="navbar-right">
				<div class="language-toggle">
					<select value={$locale} onchange={handleLanguageChange}>
						{#each locales as lang}
							<option value={lang}>{lang.toUpperCase()}</option>
						{/each}
					</select>
				</div>
				{#if user}
					<span class="user-email">{user.email}</span>
					{#if user.subscriptionType}
						<SubscriptionInfo />
					{/if}
				{/if}
				<button class="logout-button" onclick={handleLogout}>
					{m.nav_logout()}
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
{/key}

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
		background-color: white;
		color: #727271;
		padding: 0 20px;
		height: 60px;
		box-shadow: 0 1px 0 rgba(0, 0, 0, 0.1);
		border-bottom: 1px solid #e0e0e0;
	}

	.navbar-left,
	.navbar-right {
		display: flex;
		align-items: center;
		gap: 8px;
	}

	.language-toggle select {
		padding: 6px 10px;
		border: 1px solid #ced4da;
		border-radius: 4px;
		background-color: white;
		color: #727271;
		font-size: 12px;
		cursor: pointer;
		transition: border-color 0.2s;
		outline: none;
	}

	.language-toggle select:focus {
		border-color: #ea5b21;
		box-shadow: 0 0 0 2px rgba(234, 91, 33, 0.1);
	}

	.nav-button {
		display: flex;
		align-items: center;
		background: none;
		border: none;
		color: #727271;
		font-size: 14px;
		cursor: pointer;
		padding: 8px 12px;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.nav-button:hover {
		background-color: rgba(234, 91, 33, 0.1);
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
		background-color: rgba(114, 114, 113, 0.1);
		color: #727271;
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
		background-color: rgba(114, 114, 113, 0.1);
		border: none;
		color: #727271;
		padding: 8px 16px;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.logout-button:hover {
		background-color: rgba(234, 91, 33, 0.1);
	}

	.logout-button svg {
		margin-left: 8px;
	}

	.content {
		flex: 1;
		padding: 20px;
		background-color: #f5f5f5;
	}

	.navbar-left {
		display: flex;
		align-items: center;
		gap: 16px;
	}

	.logo {
		display: flex;
		align-items: center;
		margin-right: 8px;
	}

	.logo svg {
		height: 25px;
		width: auto;
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

		.navbar {
			padding: 0 10px;
		}

		.nav-button {
			padding: 8px 6px;
			font-size: 12px;
		}

		.logo svg {
			height: 18px;
		}

		.language-toggle select {
			font-size: 11px;
			padding: 4px 6px;
		}
	}

	@media (max-width: 768px) {
		.navbar-left {
			overflow-x: auto;
			-webkit-overflow-scrolling: touch;
			scrollbar-width: none; /* Firefox */
			gap: 8px;
		}

		.navbar-left::-webkit-scrollbar {
			display: none; /* Chrome, Safari, Edge */
		}

		.logo {
			margin-right: 4px;
		}

		.logo svg {
			height: 20px;
		}
	}
</style>
