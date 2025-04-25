<script lang="ts">
	import { onMount } from 'svelte';
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import { getMaterials, type Material } from '$lib/api/userClient';

	let materialList: Material[] = $state([]);
	let loading = $state(true);
	let error = $state('');

	const favoriteIds = $derived($favoritesStore.ids);

	function isFavorite(materialId: number): boolean {
		return favoriteIds.includes(materialId);
	}

	async function toggleFavorite(materialId: number) {
		if (isFavorite(materialId)) {
			await favoritesStore.removeFromFavorites(materialId);
		} else {
			await favoritesStore.addToFavorites(materialId);
		}
	}

	async function loadMaterials() {
		try {
			loading = true;
			error = '';

			const materials = await getMaterials();

			if (materials) {
				materialList = materials;
			} else {
				throw new Error('Failed to load materials');
			}
		} catch (err) {
			console.error(err);
			error = 'Failed to load materials';
		} finally {
			loading = false;
		}
	}

	onMount(async () => {
		await loadMaterials();
	});
</script>

<section>
	<h1>Materials</h1>
	<div class="debug-favorites">
		<p class="debug-title">Favorite Material IDs:</p>
		<div class="debug-ids">
			{#if favoriteIds.length === 0}
				<span class="no-favorites">No favorites selected</span>
			{:else}
				{#each favoriteIds as id}
					<span class="favorite-id">{id}</span>
				{/each}
			{/if}
		</div>
	</div>
	{#if error}
		<div class="error-message">{error}</div>
	{/if}
	{#if loading}
		<div class="loading-spinner-container">
			<div class="loading-spinner"></div>
			<p>Loading materials...</p>
		</div>
	{:else}
		<table class="materials-table">
			<thead>
				<tr>
					<th rowspan="2" class="favorite-cell"> </th>
					<th rowspan="2">ID</th>
					<th rowspan="2" class="table-material-name">Material Name</th>
					<th colspan="3">Price Today</th>
					<th rowspan="2">Last Update</th>
				</tr>
				<tr>
					<th>Average</th>
					<th>Minimum</th>
					<th>Maximum</th>
				</tr>
			</thead>
			<tbody>
				{#each materialList as material}
					<tr>
						<td class="favorite-cell">
							<button
								class="favorite-button {isFavorite(material.id) ? 'is-favorite' : ''}"
								onclick={() => toggleFavorite(material.id)}
								title={isFavorite(material.id) ? 'Remove from favorites' : 'Add to favorites'}
								aria-label={isFavorite(material.id) ? 'Remove from favorites' : 'Add to favorites'}
							>
								<svg
									xmlns="http://www.w3.org/2000/svg"
									width="16"
									height="16"
									viewBox="0 0 24 24"
									fill={isFavorite(material.id) ? 'currentColor' : 'none'}
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
								>
									<polygon
										points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"
									></polygon>
								</svg>
							</button>
						</td>
						<td>{material.id}</td>
						<td class="table-material-name"
							>{material.materialName +
								' ' +
								material.unit +
								' ' +
								material.deliveryType +
								' ' +
								material.market}</td
						>
						<td>{material.latestAvgValue}</td>
						{#if material.latestMinValue === null}
							<td>-</td>
						{:else}
							<td>{material.latestMinValue}</td>
						{/if}
						{#if material.latestMaxValue === null}
							<td>-</td>
						{:else}
							<td>{material.latestMaxValue}</td>
						{/if}
						<td>{material.lastCreatedDate}</td>
					</tr>
				{:else}
					<tr>
						<td colspan="7" class="no-data">No materials found</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</section>

<style>
	.materials-table {
		width: 100%;
		border-collapse: collapse;
		margin-top: 1rem;
	}

	.materials-table th,
	.materials-table td {
		padding: 0.75rem;
		text-align: left;
		border-bottom: 1px solid #ddd;
		vertical-align: middle;
	}

	.materials-table th {
		background-color: #f8f9fa;
		font-weight: bold;
		text-align: center;
	}

	.materials-table .table-material-name {
		text-align: left;
	}

	.materials-table td {
		text-align: center;
	}

	.materials-table tbody tr:hover {
		background-color: #f1f1f1;
	}
	.no-data {
		text-align: center;
		padding: 2rem !important;
		color: #6c757d;
	}

	.error-message {
		color: #dc3545;
		padding: 0.75rem;
		background-color: rgba(220, 53, 69, 0.1);
		border-radius: 4px;
		margin-bottom: 1rem;
	}

	.loading-spinner-container {
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 2rem;
	}

	.loading-spinner {
		width: 40px;
		height: 40px;
		border: 4px solid #f3f3f3;
		border-top: 4px solid #3498db;
		border-radius: 50%;
		animation: spin 1s linear infinite;
		margin-bottom: 1rem;
	}

	@keyframes spin {
		0% {
			transform: rotate(0deg);
		}
		100% {
			transform: rotate(360deg);
		}
	}
</style>
