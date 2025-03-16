<script lang="ts">
	import { delay, ENABLE_MOCKS, favoriteMaterials } from '$lib/mock';
	import { onMount } from 'svelte';
	import { favoritesStore } from '$lib/stores/favouritesStore';

	const favoriteIds = $derived($favoritesStore.ids);
</script>

<section>
	{favoriteIds}
</section>
