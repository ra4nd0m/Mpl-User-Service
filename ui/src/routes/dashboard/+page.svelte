<script lang="ts">
	import { delay, ENABLE_MOCKS, materials } from '$lib/mock';
	import { onMount } from 'svelte';
	import { favoritesStore } from '$lib/stores/favouritesStore';

	const favoriteIds = $derived($favoritesStore.ids);

	let favoriteMaterials: any[] = $state([]);

	onMount(() => {
		favoriteMaterials = materials.filter((material) => favoriteIds.includes(material.Id));
	});
</script>

<section>
	{favoriteIds}
</section>
<section>
	<table>
		<thead>
			<tr>
				<th rowspan="2">Date</th>
				{#each favoriteMaterials as material}
					<th colspan="3">{material.MaterialName}</th>
				{/each}
			</tr>
			<tr>
				{#each favoriteMaterials as material}
					<th>Min Price</th>
					<th>Max Price</th>
					<th>Avg Price</th>
				{/each}
			</tr>
		</thead>
		<tbody> </tbody>
	</table>
</section>

<style>
	table {
		width: 100%;
		border-collapse: collapse;
	}

	th,
	td {
		border: 1px solid #ddd;
		padding: 8px;
	}

	th {
		background-color: #f2f2f2;
		text-align: center;
	}

	th[rowspan='2'] {
		vertical-align: middle;
	}
</style>
