<script lang="ts">
	import { m, locale } from '$lib/i18n';
	import type { Material } from '$lib/api/userClient';
	import { availableCurrencies, convertCurrencyValue } from '$lib/utils/currencyHelperUtil';

	let {
		title,
		materials,
		isFavorite,
		toggleFavorite,
		getChangeClass,
		onShowPrice,
		onShowDescription,
		hasSearch,
		currencyRates,
		extraColumns = []
	}: MaterialsTableProps = $props();

	let selectedCurrency = $state('');

	let nf = $derived(Intl.NumberFormat($locale, { style: 'decimal', maximumFractionDigits: 2 }));
	let df = $derived(
		Intl.DateTimeFormat($locale, { year: 'numeric', month: '2-digit', day: '2-digit' })
	);

	type MaterialsTableProps = {
		title: string;
		materials: Material[];
		isFavorite: (materialId: number) => boolean;
		toggleFavorite: (materialId: number) => void;
		getChangeClass: (changePercent: string | null) => string;
		onShowPrice: (materialId: number) => void;
		onShowDescription: (material: Material) => void;
		hasSearch: boolean;
		currencyRates: Record<string, number>;
		extraColumns?: { localisedHeader: string; render: (material: Material) => string }[];
	};

	function formatPriceValue(rawValue: number | null | undefined, materialUnit: string): string {
		if (rawValue === null || rawValue === undefined) return '—';

		const converted = convertCurrencyValue(materialUnit, rawValue, selectedCurrency, currencyRates);
		return nf.format(converted);
	}

	function currencyLabel(value: string): string {
		return value === '' ? m.materials_currency_switcher_default() : value;
	}

	function displayUnit(unit: string): string {
		return selectedCurrency ? `${unit} (${selectedCurrency})` : unit;
	}
</script>

<div class="table-header-row">
	<h2>{title}</h2>
	<label class="currency-switcher">
		<span>{m.materials_currency_switcher_label()}</span>
		<select bind:value={selectedCurrency} aria-label={m.materials_currency_switcher_aria()}>
			{#each availableCurrencies as code (code)}
				<option value={code}>{currencyLabel(code)}</option>
			{/each}
		</select>
	</label>
</div>
<div class="materials-table-container">
	<div class="table-wrapper">
		<table class="materials-table">
			<thead>
				<tr>
					<th rowspan="2" class="favorite-cell"> </th>
					<th rowspan="2" class="table-material-name">{m.materials_table_material_name()}</th>
					<th rowspan="2">{m.materials_table_change()}</th>
					<th colspan="3">{m.materials_table_price_last()}</th>
					<th rowspan="2">{m.materials_table_last_updated()}</th>
					{#each extraColumns as column (column)}
						<th rowspan="2">{column.localisedHeader}</th>
					{/each}
					<th rowspan="2"></th>
				</tr>
				<tr>
					<th>{m.materials_table_price_average()}</th>
					<th>{m.materials_table_price_min()}</th>
					<th>{m.materials_table_price_max()}</th>
				</tr>
			</thead>
			<tbody>
				{#each materials as material (material.id)}
					<tr>
						<td class="favorite-cell">
							<button
								class="favorite-button {isFavorite(material.id) ? 'is-favorite' : ''}"
								onclick={() => toggleFavorite(material.id)}
								title={isFavorite(material.id)
									? m.materials_table_remove_from_favorites()
									: m.materials_table_add_to_favorites()}
								aria-label={isFavorite(material.id)
									? m.materials_table_remove_from_favorites()
									: m.materials_table_add_to_favorites()}
							>
								{#if isFavorite(material.id)}
									<!-- Checkmark for favorites -->
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
										<polyline points="20 6 9 17 4 12"></polyline>
									</svg>
								{:else}
									<!-- Cross for non-favorites -->
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
										<line x1="18" y1="6" x2="6" y2="18"></line>
										<line x1="6" y1="6" x2="18" y2="18"></line>
									</svg>
								{/if}
							</button>
						</td>
						<td class="table-material-name">
							<div class="material-name">
								{material.materialName}
								{displayUnit(material.unit)}
								{material.deliveryType}
								{material.market}
							</div>
						</td>
					<td class="{getChangeClass(material.changePercent)}">{material.changePercent}</td>
					<td>
						{material.latestAvgValue !== null && material.latestAvgValue !== undefined
							? formatPriceValue(material.latestAvgValue, material.unit)
							: '-'}
					</td>
					{#if material.latestMinValue === null}
						<td>—</td>
					{:else}
						<td>{formatPriceValue(material.latestMinValue, material.unit)}</td>
					{/if}
					{#if material.latestMaxValue === null}
						<td>—</td>
					{:else}
						<td>{formatPriceValue(material.latestMaxValue, material.unit)}</td>
					{/if}
					<td>{material.lastCreatedDate ? df.format(new Date(material.lastCreatedDate)) : '—'}</td>
					{#each extraColumns as column (column)}
						<td>{column.render(material)}</td>
					{/each}
					<td>
						<div class="action-buttons">
							<button
								class="info-button"
								onclick={() => onShowDescription(material)}
								aria-label={m.materials_description_button_show()}
								title={m.materials_description_button_show()}
							>
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
									<circle cx="12" cy="12" r="10"></circle>
									<line x1="12" y1="16" x2="12" y2="12"></line>
									<line x1="12" y1="8" x2="12.01" y2="8"></line>
								</svg>
							</button>
							<button
								class="show-modal"
								onclick={() => onShowPrice(material.id)}
								aria-label={m.workdesk_price_tracking_chart_price_history()}
								title={m.workdesk_price_tracking_chart_price_history()}
							>
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
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
							</button>
						</div>
					</td>
					</tr>
				{:else}
					<tr>
						<td colspan={8 + extraColumns.length} class="no-data">
							{hasSearch ? m.materials_no_results() : m.materials_not_available()}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
</div>

<style>
	.table-header-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	h2 {
		margin: 0;
		font-size: 1.5rem;
		color: #333;
		border-bottom: 1px solid #e9ecef;
		padding-bottom: 0.75rem;
		flex: 1;
	}

	.currency-switcher {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.9rem;
		color: #495057;
		white-space: nowrap;
	}

	.currency-switcher select {
		border: 1px solid #ced4da;
		background: #fff;
		border-radius: 6px;
		padding: 0.3rem 0.5rem;
		font-size: 0.9rem;
		color: #212529;
	}

	.materials-table-container {
		margin-bottom: 2rem;
		border: 1px solid #e9ecef;
		border-radius: 8px;
		overflow: hidden;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
		background-color: white;
		transition: all 0.3s ease;
	}

	.materials-table-container:hover {
		box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
	}

	.table-wrapper {
		overflow-x: auto;
		-webkit-overflow-scrolling: touch;
	}

	.materials-table {
		width: 100%;
		min-width: 800px;
		border-collapse: collapse;
		margin: 0;
		background-color: white;
	}

	.materials-table th,
	.materials-table td {
		padding: 0.75rem;
		text-align: left;
		border-bottom: 1px solid #e9ecef;
		vertical-align: middle;
		white-space: nowrap;
	}

	.materials-table th {
		background-color: #f8f9fa;
		font-weight: 600;
		text-align: center;
		color: #495057;
		position: sticky;
		top: 0;
		z-index: 1;
	}

	.materials-table .table-material-name {
		text-align: left !important;
		min-width: 250px;
		max-width: 300px;
		white-space: normal;
		word-wrap: break-word;
	}

	.materials-table .favorite-cell {
		width: 60px;
		text-align: center !important;
	}

	.materials-table td {
		text-align: center;
	}

	.materials-table td:nth-child(2) {
		text-align: left;
	}

	.materials-table tbody tr {
		transition: background-color 0.2s ease;
	}

	.materials-table tbody tr:hover {
		background-color: #f1f3f5;
	}

	.materials-table tbody tr:last-child td {
		border-bottom: none;
	}

	.material-name {
		font-weight: 600;
		color: #333;
		line-height: 1.4;
	}

	.no-data {
		text-align: center !important;
		padding: 3rem 1rem !important;
		color: #6c757d;
		background-color: #f8f9fa;
		font-style: italic;
	}

	.positive-change {
		color: #198754;
		font-weight: 600;
	}

	.negative-change {
		color: #dc3545;
		font-weight: 600;
	}

	.favorite-button {
		background: none;
		border: none;
		cursor: pointer;
		padding: 0.375rem;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 6px;
		transition: all 0.2s ease;
		width: 32px;
		height: 32px;
	}

	.favorite-button:hover {
		background-color: rgba(0, 0, 0, 0.1);
		transform: scale(1.1);
	}

	.favorite-button.is-favorite {
		background-color: rgba(25, 135, 84, 0.1);
	}

	.favorite-button.is-favorite svg {
		color: #198754;
	}

	.favorite-button:not(.is-favorite) {
		background-color: rgba(220, 53, 69, 0.1);
	}

	.favorite-button:not(.is-favorite) svg {
		color: #dc3545;
	}

	.action-buttons {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.35rem;
	}

	.info-button {
		background: none;
		border: none;
		cursor: pointer;
		color: #0d6efd;
		padding: 0.375rem;
		border-radius: 6px;
		transition: all 0.2s ease;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 32px;
		height: 32px;
	}

	.info-button:hover {
		background-color: rgba(13, 110, 253, 0.12);
		color: #0a58ca;
		transform: scale(1.1);
	}

	.show-modal {
		background: none;
		border: none;
		cursor: pointer;
		color: #6c757d;
		padding: 0.375rem;
		border-radius: 6px;
		transition: all 0.2s ease;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 32px;
		height: 32px;
	}

	.show-modal:hover {
		background-color: #e9ecef;
		color: #495057;
		transform: scale(1.1);
	}

	/* Mobile responsive styles */
	@media (max-width: 768px) {
		.table-header-row {
			align-items: flex-start;
			flex-direction: column;
			gap: 0.5rem;
		}

		h2 {
			font-size: 1.3rem;
			width: 100%;
		}

		.materials-table-container {
			margin-bottom: 1.5rem;
			border-radius: 6px;
		}

		.materials-table {
			min-width: 700px;
			font-size: 0.9rem;
		}

		.materials-table th,
		.materials-table td {
			padding: 0.625rem 0.5rem;
		}

		.materials-table .table-material-name {
			min-width: 200px;
			max-width: 220px;
		}

		.favorite-cell {
			width: 50px;
		}

		.no-data {
			padding: 2rem 1rem !important;
			font-size: 0.9rem;
		}
	}

	@media (max-width: 480px) {
		h2 {
			font-size: 1.2rem;
			text-align: center;
		}

		.materials-table-container {
			border-radius: 4px;
		}

		.materials-table {
			min-width: 650px;
			font-size: 0.85rem;
		}

		.materials-table th,
		.materials-table td {
			padding: 0.5rem 0.375rem;
		}

		.materials-table .table-material-name {
			min-width: 180px;
			max-width: 200px;
		}

		.favorite-cell {
			width: 44px;
		}

		.favorite-button,
		.show-modal {
			width: 28px;
			height: 28px;
			padding: 0.25rem;
		}

		.favorite-button svg,
		.show-modal svg {
			width: 14px;
			height: 14px;
		}

		.material-name {
			font-size: 0.85rem;
			line-height: 1.3;
		}

		.no-data {
			padding: 1.5rem 0.5rem !important;
			font-size: 0.85rem;
		}
	}

	@media (max-width: 360px) {
		h2 {
			font-size: 1.1rem;
		}

		.materials-table {
			min-width: 600px;
			font-size: 0.8rem;
		}

		.materials-table th,
		.materials-table td {
			padding: 0.5rem 0.25rem;
		}

		.materials-table .table-material-name {
			min-width: 160px;
			max-width: 180px;
		}

		.material-name {
			font-size: 0.8rem;
		}
	}
</style>
