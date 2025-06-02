<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import { goto } from '$app/navigation';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	onMount(async () => {
		if (data.token) {
			authStore.setToken(data.token);
			goto('/dashboard');
		} else {
			goto('/login');
		}
	});
</script>

<svelte:head>
	<title>Redirecting...</title>
</svelte:head>

<div>Redirecting...</div>
