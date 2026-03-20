<script lang="ts">
	let {
		groups,
		selected = $bindable(''),
		label = 'Filter by group',
		allLabel = 'All',
		onchange
	}: {
		groups: { value: string; label: string }[];
		selected?: string;
		label?: string;
		allLabel?: string;
		onchange?: (value: string) => void;
	} = $props();

	function select(value: string) {
		selected = value;
		onchange?.(value);
	}
</script>

<!-- Desktop: pill buttons -->
<div class="group-buttons">
	<span class="group-label">{label}</span>
	<button class:active={selected === ''} onclick={() => select('')}>
		{allLabel}
	</button>
	{#each groups as group (group.value)}
		<button class:active={selected === group.value} onclick={() => select(group.value)}>
			{group.label}
		</button>
	{/each}
</div>

<!-- Mobile: select dropdown -->
<div class="mobile-group-select">
	<label for="group-select">{label}</label>
	<select
		id="group-select"
		value={selected}
		onchange={(e) => select((e.target as HTMLSelectElement).value)}
	>
		<option value="">{allLabel}</option>
		{#each groups as group (group.value)}
			<option value={group.value}>{group.label}</option>
		{/each}
	</select>
</div>

<style>
	.group-buttons {
		margin-bottom: 1rem;
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem;
	}

	.group-label {
		font-weight: bold;
		margin-right: 0.5rem;
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

	/* Mobile */
	.mobile-group-select {
		display: none;
		margin-bottom: 1rem;
	}

	.mobile-group-select label {
		display: block;
		font-weight: bold;
		color: #727271;
		margin-bottom: 0.5rem;
		font-size: 0.95rem;
	}

	.mobile-group-select select {
		width: 100%;
		padding: 0.75rem;
		border: 1px solid #ced4da;
		border-radius: 4px;
		background-color: white;
		color: #727271;
		font-size: 1rem;
		cursor: pointer;
		transition: border-color 0.2s;
		outline: none;
		appearance: none;
		background-image: url('data:image/svg+xml;charset=US-ASCII,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 4 5"><path fill="%23666" d="M2 0L0 2h4zm0 5L0 3h4z"/></svg>');
		background-repeat: no-repeat;
		background-position: right 0.75rem center;
		background-size: 0.65rem auto;
	}

	.mobile-group-select select:focus {
		border-color: #ea5b21;
		box-shadow: 0 0 0 2px rgba(234, 91, 33, 0.1);
	}

	@media (max-width: 768px) {
		.group-buttons {
			display: none;
		}

		.mobile-group-select {
			display: block;
			margin-bottom: 1.25rem;
		}
	}

	@media (max-width: 480px) {
		.mobile-group-select {
			margin-bottom: 1.5rem;
		}

		.mobile-group-select select {
			padding: 1rem;
			font-size: 1rem;
			border-radius: 6px;
		}
	}
</style>
