<script lang="ts">
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import PriceTable from './components/PriceTable.svelte';
	import { m } from '$lib/i18n';

	const favoriteIds = $derived($favoritesStore.ids);
</script>

<svelte:head>
	<title>{m.workdesk_price_tracking()}</title>
	<meta name="description" content={m.workdesk_price_tracking_description()} />
</svelte:head>

<section class="price-tracking-header">
	<h1>{m.workdesk_price_tracking()}</h1>
	<p>
		{m.workdesk_price_tracking_description()}
	</p>
</section>

{#if favoriteIds.length === 0}
	<div class="no-favorites">
		<p>{m.workdesk_price_tracking_no_favorites()}</p>
	</div>
{:else}
	<div class="price-tables-container">
		{#each favoriteIds as id}
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
