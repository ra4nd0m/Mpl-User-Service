<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/authStore';
	import { login } from '$lib/api/authClient';
	import { ENABLE_MOCKS, users } from '$lib/mock';

	let email = '';
	let password = '';
	let rememberMe = false;
	let error = '';
	let loading = false;

	async function handleLogin(event: SubmitEvent) {
		event.preventDefault();
		loading = true;
		error = '';
		authStore.setLoading(true);

		try {
			const result = await login(email, password, rememberMe);
			if (result.success) {
				goto('/dashboard');
			}
			if (result.error) {
				throw new Error(result.error);
			}
		} catch (err) {
			console.error(err);
			error = 'An error occurred';
		} finally {
			loading = false;
			authStore.setLoading(false);
		}
	}
</script>

<div class="login-container">
	<form onsubmit={handleLogin}>
		<h2 class="form-title">Login</h2>
		<input type="text" placeholder="Email" bind:value={email} required />
		<input type="password" placeholder="Password" bind:value={password} required />
		<div class="remember-container">
			<input type="checkbox" id="remember" bind:checked={rememberMe} />
			<label for="remember">Remember me</label>
		</div>
		{#if error}
			<div class="error-message">{error}</div>
		{/if}
		<button type="submit" disabled={loading}>{loading ? 'Logging in...' : 'Login'}</button>

		{#if ENABLE_MOCKS}
			<div class="dev-help">
				<p>Test user: {users.test.email} / {users.test.password}</p>
				<p>Admin user: {users.admin.email} / {users.admin.password}</p>
			</div>
		{/if}
	</form>
</div>

<style>
	.login-container {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 75vh;
	}

	form {
		display: flex;
		justify-content: center;
		flex-direction: column;
		align-items: center;
		width: 300px;
		padding: 20px;
		border-radius: 8px;
		box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
	}

	input[type='text'],
	input[type='password'] {
		margin: 10px;
		padding: 5px;
		width: 200px;
	}

	.remember-container {
		display: flex;
		align-items: center;
		width: 200px;
		margin: 10px 0;
	}

	.remember-container input[type='checkbox'] {
		margin-right: 8px;
	}

	.error-message {
		color: #e74c3c;
		margin: 10px 0;
		text-align: center;
		width: 200px;
		font-size: 14px;
	}

	.dev-help {
		margin-top: 15px;
		font-size: 12px;
		color: #777;
		border-top: 1px dashed #ddd;
		padding-top: 10px;
		text-align: center;
	}

	button {
		margin: 10px;
		padding: 5px;
		width: 200px;
		background-color: #3498db;
		color: white;
		border: none;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.3s;
	}

	button:hover:not(:disabled) {
		background-color: #2980b9;
	}

	button:disabled {
		background-color: #95a5a6;
		cursor: not-allowed;
	}
</style>
