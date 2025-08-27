<script lang="ts">
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import PriceTable from './components/PriceTable.svelte';

	const favoriteIds = $derived($favoritesStore.ids.sort((a, b) => a - b));
	const widgetSettings = $derived($favoritesStore);
</script>

<svelte:head>
	<title>Price Tracking</title>
	<meta
		name="description"
		content="Track price changes for your favorite materials over time. Monitor historical data and trends."
	/>
</svelte:head>

<section class="price-tracking-header">
	<h1>Price Tracking</h1>
	<p>
		Monitor price changes over time for your favorite materials. Track historical data, identify
		trends, and make informed decisions based on price fluctuations.
	</p>
</section>

{#if favoriteIds.length === 0}
	<div class="no-favorites">
		<p>No favorite materials found. Add materials to your favorites to track their prices.</p>
	</div>
{:else}
	<div class="price-tables-container">
		{#each widgetSettings.ids as id}
			<PriceTable materialId={id} />
		{/each}
	</div>
{/if}

<style>
	.price-tracking-header {
		margin-bottom: 2rem;
		border-bottom: 1px solid #e9ecef;
		padding-bottom: 1rem;
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
	}
</style>
