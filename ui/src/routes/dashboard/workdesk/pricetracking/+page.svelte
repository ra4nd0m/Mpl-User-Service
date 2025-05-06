<script lang="ts">
	import { favoritesStore } from '$lib/stores/favouritesStore';
    import PriceTable from './components/PriceTable.svelte';

	const favoriteIds = $derived($favoritesStore.ids);

    let isLoading = $state(true);
	let error = $state<string | null>(null);
    let materialTables = $state([]);


</script>

{#each favoriteIds as id}
	<PriceTable materialId={id}/>
{/each}

{#if isLoading}
    <div class="loading-container">
        <div class="loading-spinner"></div>
        <p>Loading price data...</p>
    </div>
{:else if error}
    <div class="error-container">
        <p>{error}</p>
    </div>
{:else if materialTables.length === 0}
    <div class="no-data-container">
        <p>No data available</p>
    </div>
{:else}
    {#each materialTables as table}
        {table}
    {/each}
{/if}
