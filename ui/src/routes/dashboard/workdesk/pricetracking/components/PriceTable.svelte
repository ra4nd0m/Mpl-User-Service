<script lang="ts">
	import { getMaterialDateMetrics, getMaterialInfo } from '$lib/api/userClient';
	import type { MaterialDateMetricsResp, Material } from '$lib/api/userClient';
	import { onMount } from 'svelte';

	const materialId = $props<number>();
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let priceData = $state<MaterialDateMetricsResp[] | null>(null);
	let materialInfo = $state<Material | null>(null);

	const propertyIds = [1, 2, 3];

	// Make dates mutable state variables
	let endDate = $state(new Date().toISOString().split('T')[0]);
	let startDate = $state(
		new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
	);

	async function fetchData() {
		if (!materialId) return;

		isLoading = true;
		error = null;
		try {
			const matInfo = await getMaterialInfo(materialId);
			if (!matInfo) {
				error = 'Material not found';
				return;
			}
			materialInfo = matInfo;
			const data = await getMaterialDateMetrics(materialId, propertyIds, startDate, endDate);
			if (data) {
				priceData = data;
			} else {
				error = 'No data available';
			}
		} catch (err) {
			error = 'Error fetching data';
		} finally {
			isLoading = false;
		}
	}

	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString('ru-RU', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit'
		});
	}

	function formatPrice(price: string | null): string {
		if (!price) return '-';
		return parseFloat(price).toFixed(2);
	}

	// Date range preset functions
	function setLastWeek() {
		endDate = new Date().toISOString().split('T')[0];
		startDate = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
		fetchData();
	}

	function setLastMonth() {
		endDate = new Date().toISOString().split('T')[0];
		startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
		fetchData();
	}

	function setLast3Months() {
		endDate = new Date().toISOString().split('T')[0];
		startDate = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
		fetchData();
	}

	function applyDateRange() {
		fetchData();
	}

	onMount(fetchData);
</script>

<div class="price-table-container">
	<div class="table-header">
		<h3>
			{#if materialInfo}
				Price History: {materialInfo.materialName} ({materialInfo.unit})
			{:else}
				Price History
			{/if}
		</h3>

		<div class="date-controls">
			<div class="date-presets">
				<button class="date-btn" onclick={setLastWeek}>Last 7 days</button>
				<button class="date-btn" onclick={setLastMonth}>Last 30 days</button>
				<button class="date-btn" onclick={setLast3Months}>Last 90 days</button>
			</div>

			<div class="date-range-picker">
				<div class="date-input">
					<label for="start-date">From:</label>
					<input type="date" id="start-date" bind:value={startDate} max={endDate} />
				</div>

				<div class="date-input">
					<label for="end-date">To:</label>
					<input
						type="date"
						id="end-date"
						bind:value={endDate}
						min={startDate}
						max={new Date().toISOString().split('T')[0]}
					/>
				</div>

				<button class="apply-btn" onclick={applyDateRange}>Apply</button>
			</div>
		</div>
	</div>

	{#if isLoading}
		<div class="loading-container">
			<div class="loading-spinner"></div>
			<p>Loading price data...</p>
		</div>
	{:else if error}
		<div class="error-message">
			{error}
			<button class="retry-button" onclick={fetchData}>Retry</button>
		</div>
	{:else if priceData && priceData.length > 0}
		<table class="price-table">
			<thead>
				<tr>
					<th>Date</th>
					{#if priceData[0].propsUsed.some((s) => s === 1)}
						<th>Average Price</th>
					{/if}
					{#if priceData[0].propsUsed.some((s) => s === 2)}
						<th>Min Price</th>
					{/if}
					{#if priceData[0].propsUsed.some((s) => s === 3)}
						<th>Max Price</th>
					{/if}
					{#if priceData[0].propsUsed.some((s) => s === 4)}
						<th>Weekly Forecast</th>
					{/if}
					{#if priceData[0].propsUsed.some((s) => s === 5)}
						<th>Monthly Forecast</th>
					{/if}
					{#if priceData[0].propsUsed.some((s) => s === 6)}
						<th>Supply</th>
					{/if}
				</tr>
			</thead>
			<tbody>
				{#each priceData as item}
					<tr>
						<td>{formatDate(item.date)}</td>
						<td>{formatPrice(item.valueAvg)}</td>
						{#if item.propsUsed.some((s) => s === 2)}
							<td>{formatPrice(item.valueMin)}</td>
						{/if}
						{#if item.propsUsed.some((s) => s === 3)}
							<td>{formatPrice(item.valueMax)}</td>
						{/if}
						{#if item.propsUsed.some((s) => s === 4)}
							<td>{formatPrice(item.predWeekly)}</td>
						{/if}
						{#if item.propsUsed.some((s) => s === 5)}
							<td>{formatPrice(item.predMonthly)}</td>
						{/if}
						{#if item.propsUsed.some((s) => s === 6)}
							<td>{formatPrice(item.supply)}</td>
						{/if}
					</tr>
				{/each}
			</tbody>
		</table>
	{:else}
		<div class="no-data">
			<p>No price data available for this material.</p>
		</div>
	{/if}
</div>

<style>
	.price-table-container {
		width: 100%;
		margin-top: 1rem;
	}

	.table-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		flex-wrap: wrap;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.date-controls {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.date-presets {
		display: flex;
		gap: 0.5rem;
	}

	.date-range-picker {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.date-input {
		display: flex;
		align-items: center;
		gap: 0.25rem;
	}

	.date-input label {
		font-size: 0.875rem;
		color: #495057;
	}

	.date-input input {
		padding: 0.25rem 0.5rem;
		border: 1px solid #ced4da;
		border-radius: 4px;
		font-size: 0.875rem;
	}

	.date-btn,
	.apply-btn {
		background-color: #f8f9fa;
		border: 1px solid #ced4da;
		border-radius: 4px;
		padding: 0.375rem 0.75rem;
		font-size: 0.875rem;
		cursor: pointer;
		transition: all 0.2s;
	}

	.date-btn:hover,
	.apply-btn:hover {
		background-color: #e9ecef;
	}

	.apply-btn {
		background-color: #228be6;
		color: white;
		border-color: #228be6;
	}

	.apply-btn:hover {
		background-color: #1c7ed6;
		border-color: #1c7ed6;
	}

	h3 {
		margin-bottom: 0;
		font-size: 1.2rem;
		color: #333;
	}

	.price-table {
		width: 100%;
		border-collapse: collapse;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
		border-radius: 4px;
		overflow: hidden;
	}

	.price-table th,
	.price-table td {
		padding: 0.75rem;
		text-align: left;
		border-bottom: 1px solid #e9ecef;
	}

	.price-table th {
		background-color: #f8f9fa;
		font-weight: 600;
		color: #495057;
	}

	.price-table tbody tr:hover {
		background-color: #f1f3f5;
	}

	.loading-container {
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 2rem;
	}

	.loading-spinner {
		border: 3px solid #f3f3f3;
		border-top: 3px solid #3498db;
		border-radius: 50%;
		width: 30px;
		height: 30px;
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

	.error-message {
		padding: 1rem;
		background-color: #fee;
		color: #c33;
		border-radius: 4px;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.retry-button {
		background-color: #c33;
		color: white;
		border: none;
		padding: 0.4rem 0.8rem;
		border-radius: 4px;
		cursor: pointer;
	}

	.no-data {
		text-align: center;
		padding: 2rem;
		color: #6c757d;
		background-color: #f8f9fa;
		border-radius: 4px;
	}

	@media (max-width: 768px) {
		.table-header {
			flex-direction: column;
			align-items: flex-start;
		}

		.date-range-picker {
			flex-wrap: wrap;
		}

		.price-table {
			font-size: 0.9rem;
		}

		.price-table th,
		.price-table td {
			padding: 0.5rem;
		}
	}
</style>
