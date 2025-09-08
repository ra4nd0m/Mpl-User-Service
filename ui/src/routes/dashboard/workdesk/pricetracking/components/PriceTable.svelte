<script lang="ts">
	import {
		getMaterialDateMetrics,
		getMaterialInfo,
		getMaterialSpreadsheet
	} from '$lib/api/userClient';
	import { getWeekRange, getMonthRange, getQuarterRange, getYearRange } from '$lib/utils/dateUtil';
	import type {
		MaterialDateMetricsResp,
		Material,
		SpreadsheetReq,
		SpreadsheetReqData,
		SpreadsheetReqAvgData
	} from '$lib/api/userClient';
	import { onMount } from 'svelte';
	import { widgetSettingsStore } from '$lib/stores/widgetSettingStore';
	import ChartModal from './ChartModal.svelte';

	import { m } from '$lib/i18n';

	const materialId = $props<number>();
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let priceData = $state<MaterialDateMetricsResp[] | null>(null);
	let materialInfo = $state<Material | null>(null);
	let sortDirection = $state<'asc' | 'desc'>('desc');
	let aggregatesChosen = $state<string[]>([]);
	let filteredData = $state<FilteredData[]>([]);
	let filteredDataOrdered = $state<FilteredData[]>([]);

	type FilteredData = {
		date: string;
		value: string | null;
	};

	const defaultDateRange = (() => {
		const today = new Date();
		return {
			endDate: today.toISOString().split('T')[0],
			startDate: new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
		};
	})();

	const propertyIds = [1, 2, 3];

	// Get materialId as number for lookup
	const normalizedMaterialId = typeof materialId === 'object' ? materialId.materialId : materialId;

	// Load date settings from store or use defaults
	let endDate = $state(defaultDateRange.endDate);
	let startDate = $state(defaultDateRange.startDate);

	let isExpanded = $state(false);

	async function loadSettings() {
		try {
			await widgetSettingsStore.ready();
			const savedDateRange = widgetSettingsStore.getPriceTableDateRange(normalizedMaterialId);
			startDate = savedDateRange.startDate || defaultDateRange.startDate;
			endDate = savedDateRange.endDate || defaultDateRange.endDate;

			isExpanded = widgetSettingsStore.getPriceTableExpanded(normalizedMaterialId);
		} catch (error) {
			console.error('Failed to load settings:', error);
			// Fallback to defaults if settings are not available
			startDate = defaultDateRange.startDate;
			endDate = defaultDateRange.endDate;
			isExpanded = false;
		}
	}

	// Save current date settings to store
	function saveDateSettings() {
		try {
			widgetSettingsStore.setPriceTableDateRange(normalizedMaterialId, {
				startDate,
				endDate
			});
		} catch (error) {
			console.error('Failed to save date settings:', error);
		}
	}

	function toggleExpand() {
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
			const data = await getMaterialDateMetrics(
				materialId,
				propertyIds,
				startDate,
				endDate,
				aggregatesChosen
			);
			if (data) {
				priceData = data;
				sortByDate('desc'); // Default sort by date descending
			} else {
				error = 'No data available';
			}
		} catch (err) {
			error = 'Error fetching data';
		} finally {
			isLoading = false;
		}
	}

	function sortByDate(direction: 'asc' | 'desc') {
		if (!priceData) return;

		if (aggregatesChosen.length > 0) {
			filteredDataOrdered = [...filteredDataOrdered].reverse();
			sortDirection = direction;
		}

		const sortedData = [...priceData];

		sortedData.sort((a, b) => {
			const dateA = new Date(a.date).getTime();
			const dateB = new Date(b.date).getTime();

			return direction === 'asc' ? dateA - dateB : dateB - dateA;
		});

		priceData = sortedData;
		sortDirection = direction;
	}

	function toggleSort() {
		if (sortDirection === null || sortDirection === 'desc') {
			sortByDate('asc');
		} else {
			sortByDate('desc');
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

	async function pushAggregates(aggregate: string) {
		aggregatesChosen = [];
		aggregatesChosen.push(aggregate);
		await fetchData();
		filteredData = formatData(aggregate);
		filteredDataOrdered = filteredData;
	}

	function formatData(aggregate: string) {
		if (!priceData) return [];
		switch (aggregate) {
			case 'weekly':
				return priceData
					.filter((item) => item.weeklyAvg !== '')
					.map((item) => ({
						date: getWeekRange(item.date),
						value: item.weeklyAvg
					}));
			case 'monthly':
				return priceData
					.filter((item) => item.monthlyAvg !== '')
					.map((item) => ({
						date: getMonthRange(item.date),
						value: item.monthlyAvg
					}));
			case 'quarterly':
				return priceData
					.filter((item) => item.quarterlyAvg !== '')
					.map((item) => ({
						date: getQuarterRange(item.date),
						value: item.quarterlyAvg
					}));
			case 'yearly':
				return priceData
					.filter((item) => item.yearlyAvg !== '')
					.map((item) => ({
						date: getYearRange(item.date),
						value: item.yearlyAvg
					}));
			default:
				return [];
		}
	}

	async function getSpreadsheet() {
		const spreadsheetReqDataArr = fillSpreadsheetReqData();
		if (!spreadsheetReqDataArr) {
			alert('No data available for export');
			return;
		}
		let type: 'full' | 'weekly' | 'monthly' | 'quarterly' | 'yearly' = 'full';
		if (aggregatesChosen.length > 0) {
			type = aggregatesChosen[0] as 'weekly' | 'monthly' | 'quarterly' | 'yearly';
		}
		const spreadsheetReq: SpreadsheetReq = {
			materialName: materialInfo?.materialName || '',
			market: materialInfo?.market || '',
			unit: materialInfo?.unit || '',
			deliveryType: materialInfo?.deliveryType || '',
			data: spreadsheetReqDataArr,
			type: type
		};
		let res = await getMaterialSpreadsheet(spreadsheetReq);
		if (res === null) {
			alert('An error occurred while generating the spreadsheet');
			return;
		}
	}

	function fillSpreadsheetReqData(): SpreadsheetReqAvgData[] | SpreadsheetReqData[] {
		if (aggregatesChosen.length > 0) {
			return filteredData as SpreadsheetReqAvgData[];
		} else {
			const spreadsheetReqDataArr: SpreadsheetReqData[] =
				priceData?.map((item) => {
					return {
						date: item.date,
						valueAvg: item.valueAvg,
						valueMin: item.valueMin,
						valueMax: item.valueMax,
						predWeekly: item.predWeekly,
						predMonthly: item.predMonthly,
						supply: item.supply,
						propsUsed: item.propsUsed,
						weeklyAvg: item.weeklyAvg,
						monthlyAvg: item.monthlyAvg,
						quarterlyAvg: item.quarterlyAvg,
						yearlyAvg: item.yearlyAvg
					};
				}) ?? [];
			return spreadsheetReqDataArr;
		}
	}

	onMount(async () => {
		await loadSettings();
		fetchData();
	});
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
			<div class="action-buttons">
				<button class="download-btn" onclick={getSpreadsheet} aria-label="Download spreadsheet">
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
					>
						<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
						<polyline points="7 10 12 15 17 10"></polyline>
						<line x1="12" y1="15" x2="12" y2="3"></line>
					</svg>
					<span>{m.workdesk_price_tracking_table_export()}</span>
				</button>
				<ChartModal {priceData} {materialInfo} {filteredData} {aggregatesChosen} />
			</div>
		</div>

		{#if isExpanded}
			<div class="aggregates-controls">
				<span class="aggregates-label">{m.workdesk_price_tracking_table_show_averages()}</span>
				<div class="radio-group">
					<label class="radio-label">
						<input
							type="radio"
							name="aggregate"
							value="weekly"
							checked={aggregatesChosen.includes('weekly')}
							onchange={async () => {
								await pushAggregates('weekly');
							}}
						/>
						{m.workdesk_price_tracking_table_averages_weekly()}
					</label>
					<label class="radio-label">
						<input
							type="radio"
							name="aggregate"
							value="monthly"
							checked={aggregatesChosen.includes('monthly')}
							onchange={async () => {
								await pushAggregates('monthly');
							}}
						/>
						{m.workdesk_price_tracking_table_averages_monthly()}
					</label>
					<label class="radio-label">
						<input
							type="radio"
							name="aggregate"
							value="quarterly"
							checked={aggregatesChosen.includes('quarterly')}
							onchange={async () => {
								await pushAggregates('quarterly');
							}}
						/>
						{m.workdesk_price_tracking_table_averages_quarterly()}
					</label>
					<label class="radio-label">
						<input
							type="radio"
							name="aggregate"
							value="yearly"
							checked={aggregatesChosen.includes('yearly')}
							onchange={async () => {
								await pushAggregates('yearly');
							}}
						/>
						{m.workdesk_price_tracking_table_averages_yearly()}
					</label>
					<label class="radio-label">
						<input
							type="radio"
							name="aggregate"
							value="none"
							checked={aggregatesChosen.length === 0}
							onchange={async () => {
								aggregatesChosen = [];
								await fetchData();
							}}
						/>
						{m.workdesk_price_tracking_table_averages_none()}
					</label>
				</div>
			</div>
			<div class="date-controls">
				<div class="date-presets">
					<button class="date-btn" onclick={setLastWeek}>{m.workdesk_price_tracking_table_selector_date_last_7_days()}</button>
					<button class="date-btn" onclick={setLastMonth}>{m.workdesk_price_tracking_table_selector_date_last_30_days()}</button>
					<button class="date-btn" onclick={setLast3Months}>{m.workdesk_price_tracking_table_selector_date_last_90_days()}</button>
					<button class="date-btn reset-btn" onclick={resetDateSettings}>{m.workdesk_price_tracking_table_selector_date_reset()}</button>
				</div>

				<div class="date-range-picker">
					<div class="date-input">
						<label for="start-date">{m.workdesk_price_tracking_table_selector_date_from()}</label>
						<input type="date" id="start-date" bind:value={startDate} max={endDate} />
					</div>

					<div class="date-input">
						<label for="end-date">{m.workdesk_price_tracking_table_selector_date_to()}</label>
						<input
							type="date"
							id="end-date"
							bind:value={endDate}
							min={startDate}
							max={new Date().toISOString().split('T')[0]}
						/>
					</div>

					<button class="apply-btn" onclick={applyDateRange}>{m.workdesk_price_tracking_table_selector_date_apply()}</button>
				</div>
			</div>
		{/if}
	</div>

	{#if isExpanded}
		<div class="table-content">
			{#if isLoading}
				<div class="loading-container">
					<div class="loading-spinner"></div>
					<p>{m.workdesk_price_tracking_table_loading()}</p>
				</div>
			{:else if error}
				<div class="error-message">
					{error}
					<button class="retry-button" onclick={fetchData}>{m.workdesk_price_tracking_table_retry()}</button>
				</div>
			{:else if priceData && priceData.length > 0}
				<table class="price-table">
					<thead>
						<tr>
							<th class="sortable" onclick={toggleSort}
								>{m.workdesk_price_tracking_table_head_date()} {#if sortDirection}
									<span class="sort-indicator">
										{#if sortDirection === 'asc'}
											<svg
												xmlns="http://www.w3.org/2000/svg"
												width="12"
												height="12"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												stroke-width="2"
												stroke-linecap="round"
												stroke-linejoin="round"
											>
												<polyline points="18 15 12 9 6 15"></polyline>
											</svg>
										{:else}
											<svg
												xmlns="http://www.w3.org/2000/svg"
												width="12"
												height="12"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												stroke-width="2"
												stroke-linecap="round"
												stroke-linejoin="round"
											>
												<polyline points="6 9 12 15 18 9"></polyline>
											</svg>
										{/if}
									</span>
								{/if}</th
							>
							{#if aggregatesChosen.length === 0}
								{#if priceData[0].propsUsed.some((s) => s === 1)}
									<th>{m.workdesk_price_tracking_table_head_price_average()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 2)}
									<th>{m.workdesk_price_tracking_table_head_price_min()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 3)}
									<th>{m.workdesk_price_tracking_table_head_price_max()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 4)}
									<th>{m.workdesk_price_tracking_table_head_price_forecast_weekly()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 5)}
									<th>{m.workdesk_price_tracking_table_head_price_forecast_monthly()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 6)}
									<th>{m.workdesk_price_tracking_table_head_price_supply()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -1)}
									<th>{m.workdesk_price_tracking_table_head_price_average_weekly()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -2)}
									<th>{m.workdesk_price_tracking_table_head_price_average_monthly()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -3)}
									<th>{m.workdesk_price_tracking_table_head_price_average_quarterly()}</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -4)}
									<th>{m.workdesk_price_tracking_table_head_price_average_yearly()}</th>
								{/if}
							{:else}
								{#if aggregatesChosen.includes('weekly')}
									<th>{m.workdesk_price_tracking_table_head_price_average_weekly()}</th>
								{/if}
								{#if aggregatesChosen.includes('monthly')}
									<th>{m.workdesk_price_tracking_table_head_price_average_monthly()}</th>
								{/if}
								{#if aggregatesChosen.includes('quarterly')}
									<th>{m.workdesk_price_tracking_table_head_price_average_quarterly()}</th>
								{/if}
								{#if aggregatesChosen.includes('yearly')}
									<th>{m.workdesk_price_tracking_table_head_price_average_yearly()}</th>
								{/if}
							{/if}
						</tr>
					</thead>
					<tbody>
						{#if aggregatesChosen.length > 0}
							{#each filteredDataOrdered as item}
								<tr>
									<td>{item.date}</td>
									<td>{item.value}</td>
								</tr>
							{/each}
						{:else}
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
									{#if item.propsUsed.some((s) => s === -1)}
										<td>{formatPrice(item.weeklyAvg)}</td>
									{/if}
									{#if item.propsUsed.some((s) => s === -2)}
										<td>{formatPrice(item.monthlyAvg)}</td>
									{/if}
									{#if item.propsUsed.some((s) => s === -3)}
										<td>{formatPrice(item.quarterlyAvg)}</td>
									{/if}
									{#if item.propsUsed.some((s) => s === -4)}
										<td>{formatPrice(item.yearlyAvg)}</td>
									{/if}
								</tr>
							{/each}
						{/if}
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

	.header-right {
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

	.sortable {
		cursor: pointer;
		user-select: none;
		position: relative;
	}

	.sortable:hover {
		background-color: #e9ecef;
	}

	.sort-indicator {
		display: inline-block;
		margin-left: 5px;
		vertical-align: middle;
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

	.action-buttons {
		display: flex;
		gap: 0.5rem;
		align-items: center;
	}

	.download-btn {
		display: flex;
		align-items: center;
		gap: 0.25rem;
		background-color: #4caf50;
		color: white;
		border: none;
		border-radius: 4px;
		padding: 0.375rem 0.75rem;
		font-size: 0.875rem;
		cursor: pointer;
		transition: all 0.2s;
	}

	.download-btn:hover {
		background-color: #45a049;
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

	.aggregates-controls {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		margin-top: 0.5rem;
		padding-top: 0.5rem;
		border-top: 1px solid #e9ecef;
	}

	.aggregates-label {
		font-size: 0.875rem;
		color: #495057;
		margin-bottom: 0.25rem;
	}

	.aggregates-controls {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		margin-top: 0.5rem;
		padding-top: 0.5rem;
		border-top: 1px solid #e9ecef;
	}

	.aggregates-label {
		font-size: 0.875rem;
		color: #495057;
		margin-bottom: 0.25rem;
	}

	.radio-group {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem;
	}

	.radio-label {
		display: flex;
		align-items: center;
		gap: 0.25rem;
		font-size: 0.875rem;
		color: #495057;
		cursor: pointer;
	}

	.radio-label input {
		cursor: pointer;
	}

	.radio-label:hover {
		color: #228be6;
	}

	@media (max-width: 768px) {
		.radio-group {
			gap: 0.5rem;
		}
	}
</style>
