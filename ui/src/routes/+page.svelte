<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

	onMount(async () => {
		goto('/dashboard');
	});
</script>

<svelte:head>
	<title>Redirecting...</title>
</svelte:head>

<div>Redirecting...</div>
