<script lang="ts">
	import { onMount } from 'svelte';
	import { authStore } from '$lib/stores/auth';
	import { goto } from '$app/navigation';

	onMount(() => {
		const unsubscribe = authStore.subscribe((state) => {
			if (!state.isAuthenticated) {
				goto('/login');
			} else {
				goto('/dashboard');
			}
		});

		return () => {
			unsubscribe();
		};
	});
</script>

<div>Redirecting...</div>
