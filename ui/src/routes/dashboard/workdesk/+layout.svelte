<script lang="ts">
	import { goto } from '$app/navigation';
	import {m} from '$lib/i18n';

	let { children } = $props();

	function goToPriceTracking() {
		goto('/dashboard/workdesk/pricetracking');
	}

	function goToOverview() {
		goto('/dashboard/workdesk/overview');
	}

	function goToWorkArea3() {
		goto('/dashboard/workdesk/area3');
	}
</script>

<div class="workdesk-layout">
	<div class="subnav">
		<div class="subnav-container">
			<button class="subnav-button" onclick={goToPriceTracking}>
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
					<path
						d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"
					></path>
					<polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline>
					<line x1="12" y1="22.08" x2="12" y2="12"></line>
				</svg>
				{m.workdesk_price_tracking()}
			</button>
		</div>
	</div>

	<div class="workdesk-content">
		{@render children()}
	</div>
</div>

<style>
	.workdesk-layout {
		display: flex;
		flex-direction: column;
		height: 100%;
	}

	.subnav {
		background-color: #f8f9fa;
		border-bottom: 1px solid #e9ecef;
		padding: 0.5rem 1rem;
	}

	.subnav-container {
		display: flex;
		gap: 1rem;
		align-items: center;
		overflow-x: auto;
	}

	.subnav-button {
		display: flex;
		align-items: center;
		background: none;
		border: none;
		color: #495057;
		padding: 0.5rem 0.75rem;
		border-radius: 4px;
		font-size: 14px;
		cursor: pointer;
		transition:
			background-color 0.2s,
			color 0.2s;
		white-space: nowrap;
	}

	.subnav-button:hover {
		background-color: #e9ecef;
		color: #228be6;
	}

	.subnav-button svg {
		margin-right: 8px;
	}

	.workdesk-content {
		flex: 1;
		padding: 1rem;
	}

	@media (max-width: 768px) {
		.subnav-container {
			-webkit-overflow-scrolling: touch;
			scrollbar-width: none;
		}

		.subnav-container::-webkit-scrollbar {
			display: none;
		}

		.subnav-button {
			font-size: 13px;
			padding: 0.4rem 0.6rem;
		}
	}
</style>
