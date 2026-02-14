<script lang="ts">
	import {
		getMaterialDateMetrics,
		getMaterialInfo,
		getMaterialSpreadsheet
	} from '$lib/api/userClient';
	import { getWeekRange, getMonthRange, getQuarterRange, getYearRange } from '$lib/utils/dateUtil';
	import { authStore } from '$lib/stores/authStore';
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
	import ModalBase from '$components/ModalBase/ModalBase.svelte';

	import { m, locale } from '$lib/i18n';

	const {
		materialId,
		dndEnabled = $bindable(false),
		isFoldable = true
	} = $props<{
		materialId: number;
		dndEnabled: boolean;
		isFoldable: boolean;
	}>();

	let nf = $derived(Intl.NumberFormat($locale, { style: 'decimal', maximumFractionDigits: 2 }));
	let df = $derived(
		Intl.DateTimeFormat($locale, { year: 'numeric', month: '2-digit', day: '2-digit' })
	);

	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let priceData = $state<MaterialDateMetricsResp[] | null>(null);
	let materialInfo = $state<Material | null>(null);
	let sortDirection = $state<'asc' | 'desc'>('desc');
	let sortColumn = $state('date');
	let aggregatesChosen = $state<string[]>([]);
	let filteredData = $state<FilteredData[]>([]);
	let filteredDataOrdered = $state<FilteredData[]>([]);

	let isChartModalShown = $state(false);

	const canExportData = $derived($authStore.user?.canExportData ?? false);

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
				sortData('date', 'desc'); // Default sort by date descending
			} else {
				error = 'No data available';
			}
		} catch (err) {
			error = 'Error fetching data';
		} finally {
			isLoading = false;
		}
	}

	function sortData(column: string, direction: 'asc' | 'desc') {
		if (!priceData) return;

		sortColumn = column;
		sortDirection = direction;

		if (aggregatesChosen.length > 0) {
			// Handle sorting for aggregated data
			filteredDataOrdered = [...filteredData].sort((a, b) => {
				if (column === 'date') {
					// Sort by date ranges (using string comparison)
					return direction === 'asc' ? a.date.localeCompare(b.date) : b.date.localeCompare(a.date);
				} else {
					// Sort by value
					const valA = a.value ? parseFloat(a.value) : -Infinity;
					const valB = b.value ? parseFloat(b.value) : -Infinity;
					return direction === 'asc' ? valA - valB : valB - valA;
				}
			});
		} else {
			// Handle sorting for regular data
			priceData = [...priceData].sort((a, b) => {
				if (column === 'date') {
					const dateA = new Date(a.date).getTime();
					const dateB = new Date(b.date).getTime();
					return direction === 'asc' ? dateA - dateB : dateB - dateA;
				} else {
					// Get the correct property based on column name
					const valA = getValueForColumn(a, column);
					const valB = getValueForColumn(b, column);
					return direction === 'asc' ? valA - valB : valB - valA;
				}
			});
		}
	}

	// Helper function to get numeric value for a specific column
	function getValueForColumn(item: MaterialDateMetricsResp, column: string): number {
		const value = item[column as keyof MaterialDateMetricsResp];
		return value && typeof value === 'string' ? parseFloat(value) : -Infinity;
	}

	function toggleSort(column: string) {
		if (sortColumn === column) {
			// Toggle direction if same column
			sortData(column, sortDirection === 'asc' ? 'desc' : 'asc');
		} else {
			// Default to descending for new column
			sortData(column, 'desc');
		}
	}

	function formatDate(dateString: string): string {
		return df.format(new Date(dateString));
	}

	function formatPrice(price: string | null): string {
		if (!price || price.length === 0) return '—';
		return nf.format(Number(price));
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

	function openChartModal() {
		isChartModalShown = true;
	}

	onMount(async () => {
		if (typeof materialId !== 'number') return;
		await loadSettings();
		fetchData();
	});
</script>

<div class="price-table-container" class:full-height={!isFoldable}>
	<div class="table-header">
		<div class="header-left">
			{#if !dndEnabled && isFoldable}
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
			{/if}
			<div class="material-info">
				<h3>
					{#if materialInfo}
						{materialInfo.materialName}
						<span class="material-details">
							({materialInfo.unit}
							{#if materialInfo.deliveryType}, {materialInfo.deliveryType}{/if}
							{#if materialInfo.market}, {materialInfo.market}{/if})
						</span>
					{:else}
						{m.workdesk_price_tracking_chart_price_history()}
					{/if}
				</h3>

				{#if materialInfo}
					<div class="latest-values">
						{#if materialInfo.avalibleProps.some((s) => s === 1)}
							<span class="latest-value">
								<span class="value-label"
									>{m.workdesk_price_tracking_table_head_price_average_latest()}:</span
								>
								<span class="value-amount"
									>{formatPrice(materialInfo.latestAvgValue?.toString() ?? null)}</span
								>
								{#if materialInfo.unit}
									<span class="value-unit"> {materialInfo.unit}</span>
								{/if}
							</span>
						{/if}

						{#if materialInfo.changePercent}
							<span
								class="change-percent"
								class:positive={parseFloat(materialInfo.changePercent) > 0}
								class:negative={parseFloat(materialInfo.changePercent) < 0}
							>
								{materialInfo.changePercent}
							</span>
						{/if}

						{#if materialInfo.avalibleProps.some((s) => s === 6) && materialInfo.latestSupplyValue}
							<span class="supply-value">
								<span class="value-label"
									>{m.workdesk_price_tracking_table_head_price_supply_latest()}:</span
								>
								<span class="value-amount"
									>{formatPrice(materialInfo.latestSupplyValue?.toString() ?? null)}</span
								>
							</span>
						{/if}
					</div>
				{/if}
			</div>
		</div>

		{#if !dndEnabled}
			<div class="header-right">
				<div class="action-buttons">
					{#if canExportData}
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
					{/if}
					<button class="chart-btn" onclick={openChartModal} aria-label="View chart">
						<svg
							xmlns="http://www.w3.org/2000/svg"
							width="18"
							height="18"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2"
							stroke-linecap="round"
							stroke-linejoin="round"
						>
							<line x1="18" y1="20" x2="18" y2="10"></line>
							<line x1="12" y1="20" x2="12" y2="4"></line>
							<line x1="6" y1="20" x2="6" y2="14"></line>
						</svg>
						{m.workdesk_price_tracking_chart_view_chart()}
					</button>
				</div>
			</div>
		{/if}

		{#if isExpanded && !dndEnabled}
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
					<button class="date-btn" onclick={setLastWeek}
						>{m.workdesk_price_tracking_table_selector_date_last_7_days()}</button
					>
					<button class="date-btn" onclick={setLastMonth}
						>{m.workdesk_price_tracking_table_selector_date_last_30_days()}</button
					>
					<button class="date-btn" onclick={setLast3Months}
						>{m.workdesk_price_tracking_table_selector_date_last_90_days()}</button
					>
					<button class="date-btn reset-btn" onclick={resetDateSettings}
						>{m.workdesk_price_tracking_table_selector_date_reset()}</button
					>
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

					<button class="apply-btn" onclick={applyDateRange}
						>{m.workdesk_price_tracking_table_selector_date_apply()}</button
					>
				</div>
			</div>
		{/if}
	</div>

	{#if isExpanded && !dndEnabled}
		<div class="table-content">
			{#if isLoading}
				<div class="loading-container">
					<div class="loading-spinner"></div>
					<p>{m.workdesk_price_tracking_table_loading()}</p>
				</div>
			{:else if error}
				<div class="error-message">
					{error}
					<button class="retry-button" onclick={fetchData}
						>{m.workdesk_price_tracking_table_retry()}</button
					>
				</div>
			{:else if priceData && priceData.length > 0}
				<table class="price-table">
					<thead>
						<tr>
							<th class="sortable" onclick={() => toggleSort('date')}
								>{m.workdesk_price_tracking_table_head_date()}
								{#if sortColumn === 'date'}
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
									<th class="sortable" onclick={() => toggleSort('valueAvg')}>
										{m.workdesk_price_tracking_table_head_price_average()}
										{#if sortColumn === 'valueAvg'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 2)}
									<th class="sortable" onclick={() => toggleSort('valueMin')}>
										{m.workdesk_price_tracking_table_head_price_min()}
										{#if sortColumn === 'valueMin'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 3)}
									<th class="sortable" onclick={() => toggleSort('valueMax')}>
										{m.workdesk_price_tracking_table_head_price_max()}
										{#if sortColumn === 'valueMax'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 4)}
									<th class="sortable" onclick={() => toggleSort('predWeekly')}>
										{m.workdesk_price_tracking_table_head_price_forecast_weekly()}
										{#if sortColumn === 'predWeekly'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 5)}
									<th class="sortable" onclick={() => toggleSort('predMonthly')}>
										{m.workdesk_price_tracking_table_head_price_forecast_monthly()}
										{#if sortColumn === 'predMonthly'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === 6)}
									<th class="sortable" onclick={() => toggleSort('supply')}>
										{m.workdesk_price_tracking_table_head_price_supply()}
										{#if sortColumn === 'supply'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -1)}
									<th class="sortable" onclick={() => toggleSort('weeklyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_weekly()}
										{#if sortColumn === 'weeklyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -2)}
									<th class="sortable" onclick={() => toggleSort('monthlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_monthly()}
										{#if sortColumn === 'monthlyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -3)}
									<th class="sortable" onclick={() => toggleSort('quarterlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_quarterly()}
										{#if sortColumn === 'quarterlyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if priceData[0].propsUsed.some((s) => s === -4)}
									<th class="sortable" onclick={() => toggleSort('yearlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_yearly()}
										{#if sortColumn === 'yearlyAvg'}
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
										{/if}
									</th>
								{/if}
							{:else}
								{#if aggregatesChosen.includes('weekly')}
									<th class="sortable" onclick={() => toggleSort('weeklyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_weekly()}
										{#if sortColumn === 'weeklyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if aggregatesChosen.includes('monthly')}
									<th class="sortable" onclick={() => toggleSort('monthlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_monthly()}
										{#if sortColumn === 'monthlyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if aggregatesChosen.includes('quarterly')}
									<th class="sortable" onclick={() => toggleSort('quarterlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_quarterly()}
										{#if sortColumn === 'quarterlyAvg'}
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
										{/if}
									</th>
								{/if}
								{#if aggregatesChosen.includes('yearly')}
									<th class="sortable" onclick={() => toggleSort('yearlyAvg')}>
										{m.workdesk_price_tracking_table_head_price_average_yearly()}
										{#if sortColumn === 'yearlyAvg'}
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
										{/if}
									</th>
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

<ModalBase
	title={materialInfo
		? `${m.workdesk_price_tracking_chart_price_history()}: ${materialInfo.materialName}`
		: m.workdesk_price_tracking_chart_price_history()}
	Component={ChartModal}
	componentProps={{ priceData, materialInfo, filteredData, aggregatesChosen }}
	bind:showModal={isChartModalShown}
/>

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

	.price-table-container.full-height {
		height: 100%;
		margin-bottom: 0;
		display: flex;
		flex-direction: column;
		border: none;
		box-shadow: none;
	}

	.price-table-container.full-height .table-content {
		flex: 1;
		max-height: none;
		overflow-y: visible;
	}

	.price-table-container:hover {
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
	}

	.chart-btn {
		display: flex;
		align-items: center;
		gap: 6px;
		background-color: #f0f2f5;
		border: 1px solid #ced4da;
		border-radius: 4px;
		padding: 6px 12px;
		font-size: 14px;
		cursor: pointer;
		transition: all 0.2s;
	}

	.chart-btn:hover {
		background-color: #e9ecef;
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
		padding: 0.5rem;
		display: flex;
		align-items: center;
		justify-content: center;
		color: #495057;
		transition: transform 0.2s ease;
		min-height: 2.5rem;
		border-radius: 4px;
	}

	.toggle-button:hover {
		background-color: #f1f3f5;
	}

	.toggle-button svg {
		width: 20px;
		height: 20px;
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

		/* Hide date preset buttons on mobile to save space */
		.date-presets {
			display: none;
		}

		.date-range-picker {
			flex-wrap: wrap;
			width: 100%;
			gap: 0.75rem;
		}

		.date-input {
			flex-direction: column;
			align-items: flex-start;
			gap: 0.25rem;
			flex: 1;
			min-width: 120px;
		}

		.date-input input {
			width: 100%;
			padding: 0.5rem;
			padding-right: 1px;
			font-size: 16px; /* Prevent zoom on iOS */
		}

		.apply-btn {
			width: 100%;
			padding: 0.75rem;
			font-size: 1rem;
			margin-top: 0.5rem;
		}

		.price-table {
			font-size: 0.9rem;
		}

		.price-table th,
		.price-table td {
			padding: 0.5rem;
		}

		/* Make toggle button bigger on mobile */
		.toggle-button {
			padding: 0.75rem;
			min-height: 3rem;
		}

		.toggle-button svg {
			width: 24px;
			height: 24px;
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

	.material-info {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.latest-values {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem;
		align-items: center;
		font-size: 0.875rem;
	}

	.latest-value,
	.supply-value {
		display: flex;
		align-items: center;
		gap: 0.25rem;
		color: #495057;
	}

	.value-label {
		color: #6c757d;
		font-weight: 500;
	}

	.value-amount {
		font-weight: 600;
		color: #212529;
	}

	.value-unit {
		color: #6c757d;
		font-size: 0.8em;
	}

	.change-percent {
		display: flex;
		align-items: center;
		gap: 0.2rem;
		font-weight: 600;
		padding: 0.2rem 0.4rem;
		border-radius: 4px;
		font-size: 0.8rem;
	}

	.change-percent.positive {
		color: #198754;
		background-color: #d1eddb;
	}

	.change-percent.negative {
		color: #dc3545;
		background-color: #f8d7da;
	}

	@media (max-width: 768px) {
		.latest-values {
			gap: 0.5rem;
			font-size: 0.8rem;
		}

		.change-percent {
			font-size: 0.75rem;
			padding: 0.15rem 0.3rem;
		}
	}
</style>
