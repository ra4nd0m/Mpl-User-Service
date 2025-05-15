<script lang="ts">
	import { getMaterialDateMetrics, getMaterialInfo } from '$lib/api/userClient';
	import type { MaterialDateMetricsResp, Material } from '$lib/api/userClient';
	import { onMount } from 'svelte';
	import { widgetSettingsStore } from '$lib/stores/widgetSettingStore';
    import ChartModal from './ChartModal.svelte';

	const materialId = $props<number>();
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let priceData = $state<MaterialDateMetricsResp[] | null>(null);
	let materialInfo = $state<Material | null>(null);

	const propertyIds = [1, 2, 3];

	// Get materialId as number for lookup
	const normalizedMaterialId = typeof materialId === 'object' ? materialId.materialId : materialId;

	// Load date settings from store or use defaults
	const savedDateRange = widgetSettingsStore.getPriceTableDateRange(normalizedMaterialId);
	let endDate = $state(savedDateRange.endDate);
	let startDate = $state(savedDateRange.startDate);

    let isExpanded = $state(widgetSettingsStore.getPriceTableExpanded(normalizedMaterialId));

	// Save current date settings to store
	function saveDateSettings() {
		widgetSettingsStore.setPriceTableDateRange(normalizedMaterialId, {
			startDate,
			endDate
		});
	}

    function toggleExpand(){
        isExpanded = !isExpanded;

        widgetSettingsStore.setPriceTableExpanded(normalizedMaterialId, isExpanded);
    }

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
		saveDateSettings();
		fetchData();
	}

	function setLastMonth() {
		endDate = new Date().toISOString().split('T')[0];
		startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
		saveDateSettings();
		fetchData();
	}

	function setLast3Months() {
		endDate = new Date().toISOString().split('T')[0];
		startDate = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
		saveDateSettings();
		fetchData();
	}

	function applyDateRange() {
		saveDateSettings();
		fetchData();
	}

	function resetDateSettings() {
		widgetSettingsStore.resetPriceTableSettings(normalizedMaterialId);
		const defaultRange = widgetSettingsStore.getPriceTableDateRange(normalizedMaterialId);
		startDate = defaultRange.startDate;
		endDate = defaultRange.endDate;
		fetchData();
	}

	onMount(fetchData);
</script>

<div class="price-table-container">
    <div class="table-header">
        <div class="header-left">
            <button class="toggle-button" onclick={toggleExpand} aria-label="Toggle table visibility">
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    class={isExpanded ? 'expanded' : 'collapsed'}
                >
                    <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
            </button>
            <h3>
                {#if materialInfo}
                    {materialInfo.materialName}
                    <span class="material-details">
                        ({materialInfo.unit}
                        {#if materialInfo.deliveryType}, {materialInfo.deliveryType}{/if}
                        {#if materialInfo.market}, {materialInfo.market}{/if})
                    </span>
                {:else}
                    Price History
                {/if}
            </h3>
        </div>

        <div class="header-right">
            {#if priceData && priceData.length > 0}
                <ChartModal {priceData}{materialInfo}/>
            {/if}
        </div>

        {#if isExpanded}
            <div class="date-controls">
                <div class="date-presets">
                    <button class="date-btn" onclick={setLastWeek}>Last 7 days</button>
                    <button class="date-btn" onclick={setLastMonth}>Last 30 days</button>
                    <button class="date-btn" onclick={setLast3Months}>Last 90 days</button>
                    <button class="date-btn reset-btn" onclick={resetDateSettings}>Reset</button>
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
        {/if}
    </div>

    {#if isExpanded}
        <div class="table-content">
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
                                {#if item.propsUsed.some((s) => s === 1)}
                                    <td>{formatPrice(item.valueAvg)}</td>
                                {/if}
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
    {/if}
</div>

<style>
    .price-table-container {
        width: 100%;
        margin-bottom: 1.5rem;
        border: 1px solid #e9ecef;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
        transition: all 0.3s ease;
    }
    
    .price-table-container:hover {
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    }

    .table-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
        padding: 1rem;
        background-color: #f8f9fa;
        border-bottom: 1px solid #e9ecef;
    }
    
    .header-left {
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }

    .header-right{
        margin-left: auto;
    }
    
    .toggle-button {
        background: none;
        border: none;
        cursor: pointer;
        padding: 0.25rem;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #495057;
        transition: transform 0.2s ease;
    }
    
    .toggle-button svg.expanded {
        transform: rotate(0deg);
    }
    
    .toggle-button svg.collapsed {
        transform: rotate(-90deg);
    }
    
    .material-details {
        font-weight: normal;
        font-size: 0.9em;
        color: #6c757d;
    }

    .table-content {
        max-height: 500px;
        overflow-y: auto;
        transition: max-height 0.3s ease;
    }

    .date-controls {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .date-presets {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
    }

    .date-range-picker {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
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
    
    .reset-btn {
        background-color: #f8d7da;
        border-color: #f5c2c7;
        color: #842029;
    }
    
    .reset-btn:hover {
        background-color: #f5c2c7;
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
        margin: 0;
        font-size: 1.2rem;
        color: #333;
    }

    .price-table {
        width: 100%;
        border-collapse: collapse;
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
        position: sticky;
        top: 0;
        z-index: 1;
    }

    .price-table tbody tr:hover {
        background-color: #f1f3f5;
    }
    
    .price-table tbody tr:last-child td {
        border-bottom: none;
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
        margin: 1rem;
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
        margin: 1rem;
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