<script lang="ts">
	import { onMount } from 'svelte';
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import {
		getMaterialGroups,
		getMaterials,
		getMaterialsByGroup,
		type Material
	} from '$lib/api/userClient';

	import { m } from '$lib/i18n';

	let materialGroups: { id: number; name: string }[] = $state([]);
	let selectedGroupId: number | null = $state(null);
	let materialList: Material[] = $state([]);
	let loading = $state(true);
	let error = $state('');
	let searchQuery = $state('');

	let filteredMaterials: Material[] = $derived(
		searchQuery
			? materialList.filter(
					(material) =>
						material.materialName.toLowerCase().includes(searchQuery.toLowerCase()) ||
						material.deliveryType.toLowerCase().includes(searchQuery.toLowerCase()) ||
						material.market.toLowerCase().includes(searchQuery.toLowerCase()) ||
						material.id.toString().includes(searchQuery)
				)
			: materialList
	);

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

	async function loadMaterials(groupId: number | null = selectedGroupId) {
		try {
			loading = true;
			error = '';

			let materials: Material[] | null = null;

			if (groupId === null) {
				materials = await getMaterials();
			} else {
				materials = await getMaterialsByGroup(groupId);
			}

			if (materials) {
				materialList = materials;
			} else {
				throw new Error('Failed to load materials');
			}
		} catch (err) {
			console.error(err);
			error = 'Failed to load materials';
			materialList = [];
		} finally {
			loading = false;
		}
	}

	async function loadGroups() {
		try {
			const groups = await getMaterialGroups();
			if (groups) {
				materialGroups = groups;
			} else {
				error = 'Failed to load material groups';
			}
		} catch (err) {
			console.error(err);
			error = 'Failed to load material groups';
		}
	}

	async function selectGroup(groupId: number | null) {
		selectedGroupId = groupId;
		await loadMaterials(selectedGroupId);
	}

	function getChangeClass(changePercent: string | null): string {
		if (!changePercent) return '';

		// Remove any non-numeric characters except for the minus sign and decimal point
		const value = parseFloat(changePercent.replace(/[^\d.-]/g, ''));

		if (value > 0) return 'positive-change';
		if (value < 0) return 'negative-change';
		return '';
	}

	onMount(async () => {
		await loadGroups();
		await loadMaterials();
	});
</script>

<svelte:head>
	<title>{m.materials_header()}</title>
	<meta
		name="description"
		content="View and manage materials, filter by groups, and track favorites."
	/>
</svelte:head>

<section>
	<h1>{m.materials_header()}</h1>
	<!-- Material Group Buttons -->
	<div class="group-buttons">
		<span class="group-label">{m.materials_filter_by_group()}</span>
		<button class:active={selectedGroupId === null} onclick={() => selectGroup(null)}>
			{m.materials_group_all()}
		</button>
		{#each materialGroups as group}
			<button class:active={selectedGroupId === group.id} onclick={() => selectGroup(group.id)}>
				{group.name}
			</button>
		{/each}
	</div>

	<div class="search-container">
		<input
			type="text"
			placeholder={m.materials_search_placeholder()}
			bind:value={searchQuery}
			class="search-input"
		/>
		{#if searchQuery}
			<button class="clear-search" onclick={() => (searchQuery = '')}> x </button>
		{/if}
	</div>

	{#if error}
		<div class="error-message">{error}</div>
	{/if}
	{#if loading}
		<div class="loading-spinner-container">
			<div class="loading-spinner"></div>
			<p>{m.materials_loading()}</p>
		</div>
	{:else}
		<table class="materials-table">
			<thead>
				<tr>
					<th rowspan="2" class="favorite-cell"> </th>
					<th rowspan="2" class="table-material-name">{m.materials_table_material_name()}</th>
					<th rowspan="2">{m.materials_table_change()}</th>
					<th colspan="3">{m.materials_table_price_last()}</th>
					<th rowspan="2">{m.materials_table_last_updated()}</th>
				</tr>
				<tr>
					<th>{m.materials_table_price_average()}</th>
					<th>{m.materials_table_price_min()}</th>
					<th>{m.materials_table_price_max()}</th>
				</tr>
			</thead>
			<tbody>
				{#each filteredMaterials as material}
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
						<td class="table-material-name"
							>{material.materialName +
								' ' +
								material.unit +
								' ' +
								material.deliveryType +
								' ' +
								material.market}</td
						>
						<td class={getChangeClass(material.changePercent)}>{material.changePercent}</td>
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
						<td colspan="8" class="no-data">
							{searchQuery ? m.materials_no_results() : m.materials_not_available()}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	{/if}
</section>

<style>
	.group-buttons {
		margin-bottom: 1rem;
		display: flex;
		flex-wrap: wrap; /* Allow buttons to wrap on smaller screens */
		align-items: center;
		gap: 0.5rem;
	}

	.group-label {
		font-weight: bold;
		margin-right: 0.5rem; /* Add some space between label and buttons */
		color: #727271;
	}

	.group-buttons button {
		padding: 0.5rem 1rem;
		border: 1px solid #ced4da;
		border-radius: 4px;
		background-color: #f8f9fa;
		color: #727271;
		cursor: pointer;
		transition:
			background-color 0.2s ease-in-out,
			border-color 0.2s ease-in-out,
			color 0.2s ease-in-out;
	}

	.group-buttons button:hover {
		background-color: rgba(234, 91, 33, 0.1);
		border-color: #ea5b21;
		color: #ea5b21;
	}

	.group-buttons button.active {
		background-color: #ea5b21;
		color: white;
		border-color: #ea5b21;
		font-weight: bold;
	}

	.group-buttons button.active:hover {
		background-color: #d54e1a;
		border-color: #d54e1a;
	}

	.search-container {
		position: relative;
		margin-bottom: 1rem;
		width: 100%;
		max-width: 500px;
	}

	.search-input {
		width: 100%;
		padding: 0.75rem;
		padding-right: 2.5rem;
		border: 1px solid #ccc;
		border-radius: 4px;
		font-size: 1rem;
	}

	.clear-search {
		position: absolute;
		right: -35px;
		top: 50%;
		transform: translateY(-50%);
		background: none;
		border: none;
		font-size: 1.5rem;
		cursor: pointer;
		color: #6c757d;
	}

	.clear-search:hover {
		color: #343a40;
	}
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

	.positive-change {
		color: #2ecc71;
		font-weight: 500;
	}

	.negative-change {
		color: #e74c3c;
		font-weight: 500;
	}
</style>
