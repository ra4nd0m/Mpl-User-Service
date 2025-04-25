<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import { goto } from '$app/navigation';
	import { refreshAccessToken } from '$lib/api/authClient';
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

<div>Redirecting...</div>
