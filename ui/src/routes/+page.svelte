<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/authStore';
	import { goto } from '$app/navigation';
	import { refreshAccessToken } from '$lib/api/authClient';

	onMount(async () => {
		const newToken = await refreshAccessToken();

		if (newToken) {
			authStore.setToken(newToken);
			goto('/dashboard');
		} else {
			goto('/login');
		}
	});
</script>

<div>Redirecting...</div>
