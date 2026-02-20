<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import { authStore } from '$lib/stores/authStore';
	import AddFileModal from './AddFileModal.svelte';

	const isAdmin = $derived($authStore.roles?.includes('Admin'));
	let showAddFileModal = $state(false);

	function openAddFileModal() {
		showAddFileModal = true;
	}
</script>

<div>
	{#if isAdmin}
		<button onclick={openAddFileModal}>Open add file modal</button>
	{/if}
	<p>Reports coming soon</p>
</div>

<ModalBase bind:showModal={showAddFileModal} title="Add Files Modal" Component={AddFileModal} />
