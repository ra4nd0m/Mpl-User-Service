<script lang="ts">
	import { delay, ENABLE_MOCKS, favoriteMaterials } from '$lib/mock';
	import { onMount } from 'svelte';

	let favoriteIds: number[] = [];
	let loading = true;
	let error = '';

	async function getFavorites() {
		try {
			if (ENABLE_MOCKS) {
				await delay();
				favoriteIds = favoriteMaterials['123'];
				loading = false;
				return;
			}
		} catch (err) {
			console.error(err);
			error = 'Failed to load favorites';
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		getFavorites();
	});
</script>

<section>
    {favoriteIds}
</section>
