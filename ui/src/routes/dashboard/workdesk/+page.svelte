<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';

    onMount(() => {
        goto('/dashboard/workdesk/pricetracking');
    });
</script>

<div>
    <p>Redirecting to price tracking...</p>
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