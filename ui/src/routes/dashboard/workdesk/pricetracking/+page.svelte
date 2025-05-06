<script lang="ts">
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import PriceTable from './components/PriceTable.svelte';

	const favoriteIds = $derived($favoritesStore.ids);

</script>

{#each favoriteIds as id}
	<PriceTable materialId={id} />
{/each}

