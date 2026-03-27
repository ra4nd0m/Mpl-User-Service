<script lang="ts">
	import { locale, m } from '$lib/i18n';

	type CurrencyRateItem = {
		code: string;
		rate: number;
	};

	type CurrencyRatesWidgetProps = {
		currencies: CurrencyRateItem[];
		actualDate?: string | null;
		loading?: boolean;
		error?: string;
	};

	let { currencies, actualDate = null, loading = false, error = '' }: CurrencyRatesWidgetProps = $props();

	const intlLocale = $derived($locale === 'ru' ? 'ru-RU' : 'en-US');

	const rubFormatter = $derived(
		Intl.NumberFormat(intlLocale, {
			style: 'currency',
			currency: 'RUB',
			minimumFractionDigits: 2,
			maximumFractionDigits: 4
		})
	);

	const dateFormatter = $derived(
		Intl.DateTimeFormat(intlLocale, {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit'
		})
	);

	const hasData = $derived(currencies.length > 0);
	const formattedDate = $derived(actualDate ? dateFormatter.format(new Date(actualDate)) : '');
</script>

<aside class="currency-line" aria-label={m.materials_currency_widget_title()}>
	{#if loading}
		<p class="state">{m.materials_currency_widget_loading()}</p>
	{:else if error}
		<p class="state error">{m.materials_currency_widget_error()}</p>
	{:else if !hasData}
		<p class="state">{m.materials_currency_widget_empty()}</p>
	{:else}
		<div class="line-content">
			<span class="line-title">{m.materials_currency_widget_title()}:</span>
			<ul class="currency-inline-list" aria-label={m.materials_currency_widget_title()}>
				{#each currencies as item (item.code)}
					<li>
						<span class="code">{item.code}</span>
						<span class="value">{rubFormatter.format(item.rate)}</span>
					</li>
				{/each}
			</ul>
			{#if formattedDate}
				<span class="updated-at">{m.materials_currency_widget_updated()}: {formattedDate}</span>
			{/if}
		</div>
	{/if}
</aside>

<style>
	.currency-line {
		width: 100%;
		border: 1px solid #e9ecef;
		border-radius: 8px;
		background: #fbfcfe;
		padding: 0.5rem 0.625rem;
	}

	.line-content {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.line-title {
		font-size: 0.85rem;
		font-weight: 700;
		color: #334155;
		white-space: nowrap;
	}

	.currency-inline-list {
		list-style: none;
		padding: 0;
		margin: 0;
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.35rem;
	}

	.currency-inline-list li {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.2rem 0.45rem;
		border-radius: 999px;
		background-color: #eef3fa;
	}

	.code {
		font-weight: 700;
		color: #0f172a;
		font-size: 0.85rem;
	}

	.value {
		font-variant-numeric: tabular-nums;
		color: #334155;
		font-size: 0.85rem;
	}

	.updated-at {
		margin-left: auto;
		font-size: 0.8rem;
		color: #64748b;
		white-space: nowrap;
	}

	.state {
		margin: 0;
		color: #64748b;
		font-size: 0.85rem;
	}

	.state.error {
		color: #dc3545;
	}

	@media (max-width: 768px) {
		.currency-line {
			padding: 0.5rem;
		}

		.line-content {
			align-items: flex-start;
		}

		.updated-at {
			margin-left: 0;
			width: 100%;
		}
	}
</style>