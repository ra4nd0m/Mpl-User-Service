<script lang="ts">
	import { dndzone } from 'svelte-dnd-action';
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import PriceTable from '$components/PriceDisplay/PriceTable.svelte';
	import { m } from '$lib/i18n';

	const favoriteIds = $derived($favoritesStore.ids);
	let favoriteIdsAsItems = $derived($favoritesStore.ids.map((id) => ({ id })));
	let tempFavourites = $state<{ id: number }[]>([]);

	let dndEnabled = $state(false);
	let isDragging = $state(false);

	function handleConsider(e: CustomEvent<{ items: { id: number }[] }>) {
		if (dndEnabled) {
			tempFavourites = e.detail.items;
			isDragging = true;
		}
	}

	function handleFinalise(e: CustomEvent<{ items: { id: number }[] }>) {
		if (dndEnabled) {
			tempFavourites = e.detail.items;
			isDragging = false;
		}
	}

	function enableDnd() {
		dndEnabled = true;
		tempFavourites = [...favoriteIdsAsItems];
	}

	function confirmDndChanges() {
		favoritesStore.setFavourites(tempFavourites.map((item) => item.id));
		dndEnabled = false;
	}

	function cancelDndChanges() {
		tempFavourites = [];
		dndEnabled = false;
	}
</script>

<svelte:head>
	<title>{m.workdesk_price_tracking()}</title>
	<meta name="description" content={m.workdesk_price_tracking_description()} />
</svelte:head>

<section class="price-tracking-header">
	<div class="header-content">
		<div>
			<h1>{m.workdesk_price_tracking()}</h1>
			<p>
				{m.workdesk_price_tracking_description()}
			</p>
		</div>

		{#if favoriteIds.length > 1}
			<div class="dnd-controls">
				{#if !dndEnabled}
					<button class="btn-reorder" onclick={enableDnd}>
						{m.workdesk_price_tracking_reorder_tables_btn()}
					</button>
				{:else}
					<div class="dnd-actions">
						<button class="btn-confirm" onclick={confirmDndChanges}>
							{m.universal_confirm_btn()}
						</button>
						<button class="btn-cancel" onclick={cancelDndChanges}>
							{m.universal_cancel_btn()}
						</button>
					</div>
				{/if}
			</div>
		{/if}
	</div>
</section>

{#if favoriteIds.length === 0}
	<div class="no-favorites">
		<p>{m.workdesk_price_tracking_no_favorites()}</p>
	</div>
{:else}
	<div
		class="price-tables-container"
		use:dndzone={{
			items: dndEnabled ? tempFavourites : favoriteIdsAsItems,
			flipDurationMs: 300,
			dragDisabled: !dndEnabled
		}}
		onconsider={handleConsider}
		onfinalize={handleFinalise}
		class:dnd-enabled={dndEnabled}
		class:dnd-active={isDragging}
	>
		{#each dndEnabled ? tempFavourites : favoriteIdsAsItems as { id } (id)}
			<PriceTable materialId={id} bind:dndEnabled isFoldable={true} />
		{/each}
	</div>
{/if}

<style>
	.price-tracking-header {
		margin-bottom: 2rem;
		border-bottom: 1px solid #e9ecef;
		padding-bottom: 1rem;
	}

	.header-content {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: 2rem;
	}

	.price-tracking-header h1 {
		margin-bottom: 0.5rem;
		font-size: 1.8rem;
		color: #333;
	}

	.price-tracking-header p {
		color: #6c757d;
		max-width: 800px;
		line-height: 1.5;
	}

	.dnd-controls {
		flex-shrink: 0;
	}

	.btn-reorder {
		background-color: #007bff;
		color: white;
		border: none;
		border-radius: 4px;
		padding: 0.5rem 1rem;
		font-size: 0.9rem;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.btn-reorder:hover {
		background-color: #0056b3;
	}

	.dnd-actions {
		display: flex;
		gap: 0.5rem;
	}

	.btn-confirm {
		background-color: #28a745;
		color: white;
		border: none;
		border-radius: 4px;
		padding: 0.5rem 1rem;
		font-size: 0.9rem;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.btn-confirm:hover {
		background-color: #1e7e34;
	}

	.btn-cancel {
		background-color: #dc3545;
		color: white;
		border: none;
		border-radius: 4px;
		padding: 0.5rem 1rem;
		font-size: 0.9rem;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.btn-cancel:hover {
		background-color: #c82333;
	}

	.no-favorites {
		background-color: #f8f9fa;
		padding: 2rem;
		border-radius: 4px;
		text-align: center;
		color: #6c757d;
	}

	.price-tables-container {
		display: flex;
		flex-direction: column;
		gap: 2rem;
		transition: all 0.3s ease;
	}

	.price-tables-container.dnd-enabled {
		background-color: #f8f9fa;
		border: 2px dashed #007bff;
		border-radius: 8px;
		padding: 1rem;
	}

	.price-tables-container.dnd-active {
		background-color: #e3f2fd;
		border-color: #1976d2;
		box-shadow: 0 4px 12px rgba(0, 123, 255, 0.2);
	}

	.price-tables-container.dnd-enabled :global(.price-table) {
		cursor: grab;
		transition:
			transform 0.2s,
			box-shadow 0.2s;
	}

	.price-tables-container.dnd-active :global(.price-table) {
		cursor: grabbing;
	}

	.price-tables-container.dnd-enabled :global(.price-table:hover) {
		transform: translateY(-2px);
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
	}

	/* Mobile responsive styles */
	@media (max-width: 768px) {
		.header-content {
			flex-direction: column;
			align-items: stretch;
			gap: 1rem;
		}

		.dnd-controls {
			align-self: flex-end;
		}

		.dnd-actions {
			justify-content: flex-end;
		}

		.price-tracking-header h1 {
			font-size: 1.5rem;
		}

		.price-tracking-header p {
			font-size: 0.9rem;
		}
	}

	@media (max-width: 480px) {
		.header-content {
			gap: 0.75rem;
		}

		.dnd-controls {
			align-self: stretch;
		}

		.btn-reorder,
		.btn-confirm,
		.btn-cancel {
			width: 100%;
			padding: 0.75rem 1rem;
			font-size: 0.9rem;
		}

		.dnd-actions {
			flex-direction: column;
			gap: 0.5rem;
			justify-content: stretch;
		}

		.price-tracking-header h1 {
			font-size: 1.3rem;
		}

		.price-tracking-header {
			margin-bottom: 1.5rem;
			padding-bottom: 0.75rem;
		}

		.price-tables-container {
			gap: 1.5rem;
		}

		.price-tables-container.dnd-enabled {
			padding: 0.75rem;
		}
	}
</style>
