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

	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import PriceTable from '$components/PriceDisplay/PriceTable.svelte';
	import MaterialsTable from './MaterialsTable.svelte';
	import GroupSelector from '$components/GroupSelector/GroupSelector.svelte';

	let isModalShown = $state(false);
	let selectedMaterialId = $state<number | null>(null);

	let materialGroups: { id: number; name: string }[] = $state([]);
	let materialList: Material[] = $state([]);
	let loading = $state(true);
	let error = $state('');
	let searchQuery = $state('');

	let selectedGroupStr = $state('');
	const groupOptions = $derived(
		materialGroups.map((g) => ({ value: String(g.id), label: g.name }))
	);

	function matchesSearch(material: Material, query: string) {
		const lowerQuery = query.toLowerCase();
		return (
			material.materialName.toLowerCase().includes(lowerQuery) ||
			material.deliveryType.toLowerCase().includes(lowerQuery) ||
			material.market.toLowerCase().includes(lowerQuery) ||
			material.id.toString().includes(lowerQuery)
		);
	}

	const isLme = (material: Material) => material.source === 'lme.com';
	const isShfe = (material: Material) => material.source === 'shfe.com';

	const lmeMaterials = $derived(materialList.filter(isLme));
	const shfeMaterials = $derived(materialList.filter(isShfe));
	const otherMaterials = $derived(
		materialList.filter((material) => !isLme(material) && !isShfe(material))
	);

	const filteredLmeMaterials = $derived(
		searchQuery
			? lmeMaterials.filter((material) => matchesSearch(material, searchQuery))
			: lmeMaterials
	);
	const filteredShfeMaterials = $derived(
		searchQuery
			? shfeMaterials.filter((material) => matchesSearch(material, searchQuery))
			: shfeMaterials
	);
	const filteredOtherMaterials = $derived(
		searchQuery
			? otherMaterials.filter((material) => matchesSearch(material, searchQuery))
			: otherMaterials
	);

	const shfeExtraColumns = [
		{
			localisedHeader: m.materials_table_volume(),
			render: (material: Material) => material.volume ?? '—'
		},
		{
			localisedHeader: m.materials_table_open_interest(),
			render: (material: Material) => material.openInterest ?? '—'
		}
	];

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

	async function loadMaterials(groupId: number | null = null) {
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
		await loadMaterials(groupId);
	}

	function getChangeClass(changePercent: string | null): string {
		if (!changePercent) return '';

		// Remove any non-numeric characters except for the minus sign and decimal point
		const value = parseFloat(changePercent.replace(/[^\d.-]/g, ''));

		if (value > 0) return 'positive-change';
		if (value < 0) return 'negative-change';
		return '';
	}

	function showPriceModal(materialId: number) {
		selectedMaterialId = materialId;
		isModalShown = true;
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
	<!-- Material Group Selector -->
	<GroupSelector
		bind:selected={selectedGroupStr}
		groups={groupOptions}
		label={m.materials_filter_by_group()}
		allLabel={m.materials_group_all()}
		onchange={(v) => selectGroup(v === '' ? null : parseInt(v, 10))}
	/>

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
		{#if shfeMaterials.length > 0}
			<MaterialsTable
				title="SHFE"
				materials={filteredShfeMaterials}
				{isFavorite}
				{toggleFavorite}
				{getChangeClass}
				onShowPrice={showPriceModal}
				hasSearch={!!searchQuery}
			/>
		{/if}
		{#if lmeMaterials.length > 0}
			<MaterialsTable
				title="LME"
				materials={filteredLmeMaterials}
				{isFavorite}
				{toggleFavorite}
				{getChangeClass}
				onShowPrice={showPriceModal}
				hasSearch={!!searchQuery}
			/>
		{/if}
		<MaterialsTable
			title={m.materials_group_other()}
			materials={filteredOtherMaterials}
			{isFavorite}
			{toggleFavorite}
			{getChangeClass}
			onShowPrice={showPriceModal}
			hasSearch={!!searchQuery}
		/>
	{/if}
</section>

{#if isModalShown && selectedMaterialId !== null}
	<ModalBase
		bind:showModal={isModalShown}
		title={m.workdesk_price_tracking_chart_price_history()}
		Component={PriceTable}
		componentProps={{ materialId: selectedMaterialId, dndEnabled: false, isFoldable: false }}
		size={{ width: '1280px', height: '80vh' }}
	/>
{/if}

<style>
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
		box-sizing: border-box;
	}

	.clear-search {
		position: absolute;
		right: 0.5rem;
		top: 50%;
		transform: translateY(-50%);
		background: none;
		border: none;
		font-size: 1.125rem;
		cursor: pointer;
		color: #6c757d;
		padding: 0.25rem;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 50%;
		width: 1.5rem;
		height: 1.5rem;
		transition: all 0.2s ease;
	}

	.clear-search:hover {
		color: #343a40;
		background-color: rgba(52, 58, 64, 0.1);
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

	/* Mobile responsive styles */
	@media (max-width: 768px) {
		.search-container {
			margin-bottom: 1.25rem;
			max-width: 100%;
		}

		.search-input {
			padding: 0.75rem;
			padding-right: 2.75rem;
			font-size: 1rem;
		}

		.clear-search {
			right: 0.625rem;
			font-size: 1rem;
		}

		h1 {
			font-size: 1.5rem;
			margin-bottom: 1.25rem;
		}
	}

	@media (max-width: 480px) {
		.search-container {
			margin-bottom: 1.5rem;
		}

		.search-input {
			padding: 1rem;
			padding-right: 3rem;
			font-size: 1rem;
			border-radius: 6px;
		}

		.clear-search {
			right: 0.75rem;
			font-size: 1.125rem;
		}

		h1 {
			font-size: 1.4rem;
			text-align: center;
			margin-bottom: 1.5rem;
		}

		.error-message {
			font-size: 0.9rem;
		}

		.loading-spinner-container {
			padding: 3rem 1rem;
		}
	}

	@media (max-width: 360px) {
		.search-input {
			padding: 0.9rem;
			padding-right: 2.5rem;
		}

		.clear-search {
			right: 0.5rem;
			font-size: 1rem;
		}

		h1 {
			font-size: 1.3rem;
		}
	}
</style>
