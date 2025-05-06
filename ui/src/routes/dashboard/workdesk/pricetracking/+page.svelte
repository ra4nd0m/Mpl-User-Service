<script lang="ts">
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import PriceTable from './components/PriceTable.svelte';

	const favoriteIds = $derived($favoritesStore.ids.sort((a, b) => a - b));
</script>

{#if favoriteIds.length === 0}
	<p>No favorite materials found.</p>
{:else}
	{#each favoriteIds as id}
		<PriceTable materialId={id} />
	{/each}
{/if}
