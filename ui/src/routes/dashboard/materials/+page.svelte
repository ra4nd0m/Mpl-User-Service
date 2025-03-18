<script lang="ts">
	import { onMount } from 'svelte';
	import { ENABLE_MOCKS, mockMaterials, delay } from '$lib/mock';
	import { fetchWithAuth } from '$lib/api/authClient';
	import { favoritesStore } from '$lib/stores/favouritesStore';

	interface Material {
		Id: number;
		MaterialName: string;
		Source: string;
		DeliveryType: string;
		Group: string;
		Market: string;
		Unit: string;
		LastCreatedDate: string | null;
	}

	let materialList: Material[] = $state([]);
	let loading = $state(true);
	let error = $state('');

	const favoriteIds = $derived($favoritesStore.ids);

	function isFavorite(materialId: number): boolean {
		return favoriteIds.includes(materialId);
	}

	async function toggleFavorite(materialId: number) {
		await favoritesStore.toggleFavorite(materialId);
	}

	async function loadMaterials() {
		try {
			if (ENABLE_MOCKS) {
				await delay();
				materialList = mockMaterials;
				loading = false;
				return;
			}
			const response = await fetchWithAuth('/userapi/data/materialList');
			if (!response.ok) {
				throw new Error('Failed to load materials');
			}
			materialList = await response.json();
		} catch (err) {
			console.error(err);
			error = 'Failed to load materials';
		} finally {
			loading = false;
		}
	}

	onMount(() => loadMaterials());
</script>

<section>
	<h1>Materials</h1>
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
					<td class="favorite-cell"> </td>
					<th>ID</th>
					<th>Material Name</th>
					<th>Source</th>
					<th>Delivery Type</th>
					<th>Group</th>
					<th>Market</th>
					<th>Unit</th>
					<th>Last Update</th>
				</tr>
			</thead>
			<tbody>
				{#each materialList as material}
					<tr>
						<td class="favorite-cell">
							<button
								class="favorite-button {isFavorite(material.Id) ? 'is-favorite' : ''}"
								onclick={() => toggleFavorite(material.Id)}
								title={isFavorite(material.Id) ? 'Remove from favorites' : 'Add to favorites'}
								aria-label={isFavorite(material.Id) ? 'Remove from favorites' : 'Add to favorites'}
							>
								<svg
									xmlns="http://www.w3.org/2000/svg"
									width="16"
									height="16"
									viewBox="0 0 24 24"
									fill={isFavorite(material.Id) ? 'currentColor' : 'none'}
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
						<td>{material.Id}</td>
						<td>{material.MaterialName}</td>
						<td>{material.Source}</td>
						<td>{material.DeliveryType}</td>
						<td>{material.Group}</td>
						<td>{material.Market}</td>
						<td>{material.Unit}</td>
						<td>{material.LastCreatedDate}</td>
					</tr>
				{:else}
					<tr>
						<td colspan="8" class="no-data">No materials found</td>
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
	}

	.materials-table th {
		background-color: #f8f9fa;
		font-weight: bold;
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
