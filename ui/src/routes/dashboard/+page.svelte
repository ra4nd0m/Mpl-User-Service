<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

	onMount(() => {
		goto('/dashboard/workdesk');
	});
</script>

<svelte:head>
	<title>Redirecting...</title>
</svelte:head>

<div>
	<p>Redirecting to workdesk...</p>
</div>

<style>
	div {
		display: flex;
		justify-content: center;
		align-items: center;
		height: 100vh;
		font-size: 1rem;
		color: #6c757d;
	}
</style>
