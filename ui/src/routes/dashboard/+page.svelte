<script lang="ts">
	import { onMount } from 'svelte';
	import { favoritesStore } from '$lib/stores/favouritesStore';
	import { dateRangeStore } from '$lib/stores/dateRangeStore';
	import {
		type Material,
		type DateGroupedMaterialValues,
		getMaterials,
		getOverview
	} from '$lib/api/userClient';

	const favoriteIds = $derived($favoritesStore.ids);
	let favoriteMaterials = $state<Material[]>([]);
	let materialData = $state<DateGroupedMaterialValues[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	//Date range for fetching,
	let startDate = $state($dateRangeStore.startDate);
	let endDate = $state($dateRangeStore.endDate);

	// Property IDs to fetch, hardocoded for now
	const propertyIds = [1, 2, 3, 6];

	// Format date for display
	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	async function fetchData() {
		error = null;
		isLoading = true;

		try {
			if (favoriteIds.length > 0) {
				const data = await getOverview(favoriteIds, propertyIds, startDate, endDate);
				if (data) {
					materialData = data.sort(
						(a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()
					);
				} else {
					error = 'Failed to fetch data';
				}
			} else {
				materialData = [];
			}
		} catch (err) {
			console.error('Error fetching data', err);
			error = 'Failed to fetch data';
		} finally {
			isLoading = false;
		}
	}

	async function handleDateChange() {
		dateRangeStore.setDateRange(startDate, endDate);
		await fetchData();
	}

	onMount(async () => {
		error = null;
		isLoading = true;
		//Explicitly load favorites
		await favoritesStore.loadFavourites();
		// Get favorite materials info
		const materialList = await getMaterials();
		if (!materialList) {
			error = 'Failed to fetch materials';
			return;
		}
		favoriteMaterials = materialList.filter((material: Material) =>
			favoriteIds.includes(material.id)
		);
		//Initial data fetch
		await fetchData();
	});
</script>

<section class="dashboard-heading">
	<h1>Dashboard</h1>
	<p>Showing market values for your {favoriteMaterials.length} favorite materials</p>

	<!-- Date range selector -->
	<div class="date-controls">
		<div class="date-inputs">
			<div class="date-field">
				<label for="start-date">Start Date</label>
				<input type="date" id="start-date" bind:value={startDate} max={endDate} />
			</div>
			<div class="date-field">
				<label for="end-date">End Date</label>
				<input type="date" id="end-date" bind:value={endDate} min={startDate} />
			</div>
		</div>
		<button class="update-btn" onclick={handleDateChange}>Update</button>
	</div>
</section>

<section>
	{#if isLoading}
		<div class="loading">Loading data...</div>
	{:else if materialData.length === 0}
		<div class="no-data">No data available</div>
	{:else if error}
		<div class="error">{error}</div>
	{:else}
		<div class="table-container">
			<table>
				<thead>
					<tr>
						<th rowspan="2">Date</th>
						{#each favoriteMaterials as material}
							<th colspan="3">{material.materialName}</th>
						{/each}
					</tr>
					<tr>
						{#each favoriteMaterials as material}
							<th>Min</th>
							<th>Max</th>
							<th>Avg</th>
						{/each}
					</tr>
				</thead>
				<tbody>
					{#each materialData as entry}
						<tr>
							<td class="date-cell">{formatDate(entry.date)}</td>

							{#each favoriteMaterials as favMaterial}
								<!-- Find if we have data for this material on this date -->
								{@const matchedData = entry.materialValues.find(
									(mv) => mv.materialInfo.id === favMaterial.id
								)}

								{#if matchedData}
									<td class="value-cell">{matchedData.valueMin || 'N/A'}</td>
									<td class="value-cell">{matchedData.valueMax || 'N/A'}</td>
									<td class="value-cell">{matchedData.valueAvg || 'N/A'}</td>
								{:else}
									<td class="value-cell no-data-cell">-</td>
									<td class="value-cell no-data-cell">-</td>
									<td class="value-cell no-data-cell">-</td>
								{/if}
							{/each}
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</section>

<style>
	.dashboard-heading {
		margin-bottom: 20px;
	}

	h1 {
		margin-bottom: 0.5rem;
	}

	p {
		color: #666;
	}

	.table-container {
		overflow-x: auto;
		margin-top: 1rem;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
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

	.date-cell {
		font-weight: 500;
		background-color: #fafafa;
	}

	.value-cell {
		text-align: right;
	}

	.value-cell.no-data-cell {
		color: #aaa;
		text-align: center;
	}

	.loading {
		display: flex;
		justify-content: center;
		padding: 2rem;
		color: #666;
	}

	.no-data {
		display: flex;
		justify-content: center;
		padding: 2rem;
		color: #666;
		background-color: #f9f9f9;
		border-radius: 4px;
	}

	.date-controls {
		display: flex;
		align-items: flex-end;
		gap: 1rem;
		margin-top: 1rem;
		background-color: #f9f9f9;
		padding: 1rem;
		border-radius: 4px;
	}

	.date-inputs {
		display: flex;
		gap: 1rem;
		flex: 1;
	}

	.date-field {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.date-field label {
		font-size: 0.8rem;
		color: #666;
	}

	.date-field input {
		padding: 0.5rem;
		border: 1px solid #ddd;
		border-radius: 4px;
	}

	.update-btn {
		background-color: #4caf50;
		color: white;
		border: none;
		border-radius: 4px;
		padding: 0.5rem 1rem;
		cursor: pointer;
		font-weight: 500;
	}

	.update-btn:hover {
		background-color: #45a049;
	}

	.error {
		padding: 1rem;
		background-color: #ffebee;
		color: #c62828;
		border-radius: 4px;
		margin-bottom: 1rem;
	}
</style>
