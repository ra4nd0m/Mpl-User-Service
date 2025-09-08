<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import type { User } from '$lib/stores/authStore';
	import { m } from '$lib/i18n';

	let isOpen = $state(false);
	let user = $state<User | null>(null);

	// Format date for display
	function formatDate(dateString: string | undefined): string {
		if (!dateString) return 'N/A';
		return new Date(dateString).toLocaleDateString('ru-RU', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit'
		});
	}

	// Calculate days remaining until subscription ends
	function getDaysRemaining(endDate: string | undefined): number | null {
		if (!endDate) return null;

		const end = new Date(endDate);
		const today = new Date();
		const diffTime = end.getTime() - today.getTime();
		return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
	}

	// Toggle dropdown
	function toggleDropdown() {
		isOpen = !isOpen;
	}

	// Close dropdown when clicking outside
	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (isOpen && !target.closest('.subscription-info')) {
			isOpen = false;
		}
	}

	onMount(() => {
		user = $authStore.user;

		// Add click event listener to document
		document.addEventListener('click', handleClickOutside);

		return () => {
			document.removeEventListener('click', handleClickOutside);
		};
	});
</script>

<div class="subscription-info">
	<button
		class="subscription-badge {user?.subscriptionType?.toLowerCase() || 'free'}"
		onclick={toggleDropdown}
	>
		{user?.subscriptionType || 'Free'}
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
			class={isOpen ? 'expanded' : 'collapsed'}
		>
			<polyline points="6 9 12 15 18 9"></polyline>
		</svg>
	</button>

	{#if isOpen}
		<div class="dropdown-menu">
			<div class="dropdown-header">
				<h4>{m.nav_subscription_details()}</h4>
			</div>
			<div class="dropdown-content">
				<div class="detail-row">
					<span class="detail-label">{m.nav_subscription_type()}:</span>
					<span class="detail-value {user?.subscriptionType?.toLowerCase() || 'free'}"
						>{user?.subscriptionType || 'Free'}</span
					>
				</div>
				<div class="detail-row">
					<span class="detail-label">{m.nav_subscription_expires()}</span>
					<span class="detail-value end-date">{formatDate(user?.subscriptionEnd)}</span>
				</div>
				{#if getDaysRemaining(user?.subscriptionEnd)}
					<div class="detail-row">
						<span class="detail-label">Time Left:</span>
						<span class="detail-value days-remaining">
							{getDaysRemaining(user?.subscriptionEnd)} days
						</span>
					</div>
				{/if}
			</div>
		</div>
	{/if}
</div>

<style>
	.subscription-info {
		position: relative;
		display: inline-block;
	}

	.subscription-badge {
		color: white;
		font-size: 12px;
		padding: 4px 8px;
		border-radius: 12px;
		background-color: #3498db;
		margin-right: 12px;
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

	.subscription-badge svg {
		transition: transform 0.2s ease;
	}

	.subscription-badge svg.expanded {
		transform: rotate(180deg);
	}

	.dropdown-menu {
		position: absolute;
		right: 0;
		top: calc(100% + 5px);
		width: 240px;
		background-color: white;
		border-radius: 6px;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
		z-index: 100;
		overflow: hidden;
		animation: fadeIn 0.2s ease;
	}

	.dropdown-header {
		padding: 12px 16px;
		border-bottom: 1px solid #e9ecef;
	}

	.dropdown-header h4 {
		margin: 0;
		font-size: 1rem;
		color: #343a40;
	}

	.dropdown-content {
		padding: 12px 16px;
	}

	.detail-row {
		display: flex;
		justify-content: space-between;
		margin-bottom: 8px;
	}

	.detail-row:last-child {
		margin-bottom: 0;
	}

	.detail-label {
		color: #6c757d;
		font-size: 0.875rem;
	}

	.detail-value {
		font-weight: 500;
		font-size: 0.875rem;
	}

	.detail-value.end-date {
		color: #6c757d;
	}

	.detail-value.free {
		color: #495057;
	}

	.detail-value.basic {
		color: #228be6;
	}

	.detail-value.premium {
		color: #e64980;
	}

	.days-remaining {
		color: #2b8a3e;
	}

	.dropdown-footer {
		padding: 12px 16px;
		border-top: 1px solid #e9ecef;
		text-align: center;
	}

	.manage-link {
		color: #228be6;
		text-decoration: none;
		font-size: 0.875rem;
		font-weight: 500;
	}

	.manage-link:hover {
		text-decoration: underline;
	}

	@keyframes fadeIn {
		from {
			opacity: 0;
			transform: translateY(-10px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}
</style>
