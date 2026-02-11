<script lang="ts">
	import { m, locale } from '$lib/i18n';
	import type { Material } from '$lib/api/userClient';

	let {
		title,
		materials,
		isFavorite,
		toggleFavorite,
		getChangeClass,
		onShowPrice,
		hasSearch,
		extraColumns = []
	}: MaterialsTableProps = $props();

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
		hasSearch: boolean;
		extraColumns?: { localisedHeader: string; render: (material: Material) => string }[];
	};
</script>

<h2>{title}</h2>
<table class="materials-table">
	<thead>
		<tr>
			<th rowspan="2" class="favorite-cell"> </th>
			<th rowspan="2" class="table-material-name">{m.materials_table_material_name()}</th>
			<th rowspan="2">{m.materials_table_change()}</th>
			<th colspan="3">{m.materials_table_price_last()}</th>
			<th rowspan="2">{m.materials_table_last_updated()}</th>
			{#each extraColumns as column}
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
		{#each materials as material}
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
				<td
					>{material.latestAvgValue !== null && material.latestAvgValue !== undefined
						? nf.format(material.latestAvgValue)
						: '-'}</td
				>
				{#if material.latestMinValue === null}
					<td>—</td>
				{:else}
					<td>{material.latestMinValue !== undefined ? nf.format(material.latestMinValue) : '—'}</td
					>
				{/if}
				{#if material.latestMaxValue === null}
					<td>—</td>
				{:else}
					<td>{material.latestMaxValue !== undefined ? nf.format(material.latestMaxValue) : '—'}</td
					>
				{/if}
				<td>{material.lastCreatedDate ? df.format(new Date(material.lastCreatedDate)) : '—'}</td>
				{#each extraColumns as column}
					<td>{column.render(material)}</td>
				{/each}
				<td
					><button
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
						</svg></button
					></td
				>
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

	.positive-change {
		color: #2ecc71;
		font-weight: 500;
	}

	.negative-change {
		color: #e74c3c;
		font-weight: 500;
	}

	.favorite-button {
		background: none;
		border: none;
		cursor: pointer;
		padding: 0.25rem;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 4px;
		transition: background-color 0.2s ease;
	}

	.favorite-button:hover {
		background-color: rgba(0, 0, 0, 0.1);
	}

	.favorite-button.is-favorite svg {
		color: #2ecc71;
	}

	.favorite-button:not(.is-favorite) svg {
		color: #e74c3c;
	}

	.show-modal {
		background: none;
		border: none;
		cursor: pointer;
		color: #6c757d;
		padding: 0.25rem;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.show-modal:hover {
		background-color: #f8f9fa;
	}
</style>
