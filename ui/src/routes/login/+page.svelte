<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/auth';

	let email = '';
	let password = '';
	let rememberMe = false;
	let error = '';
	let loading = false;

	const mockUser = {
		email: 'test@example.com',
		password: 'password123',
		token:
			'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ0ZXN0QGV4YW1wbGUuY29tIiwiU3Vic2NyaXB0aW9uVHlwZSI6IlByZW1pdW0iLCJTdWJzY3JpcHRpb25FbmQiOiIyMDI1LTEyLTMxIiwiZXhwIjoxNzE2NDcyNjQ3fQ.d7dJF4JJR7KQkM9jkZiXYx1rX-MDCvNRzAVYDLfFH2I'
	};

	async function handleLogin(event: SubmitEvent) {
		event.preventDefault();
		loading = true;
		error = '';
		authStore.setLoading(true);

		try {
			if (import.meta.env.DEV) {
				if (email === mockUser.email && password === mockUser.password) {
					await new Promise((resolve) => setTimeout(resolve, 800));
					authStore.setToken(mockUser.token);
					goto('/dashboard');
					return;
				} else if (email !== mockUser.email || password !== mockUser.password) {
					throw new Error('Invalid email or password');
				}
			}
			const response = await fetch('http://localhost:3000/api/login', {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({ email, password, rememberMe }),
				credentials: 'include'
			});
			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.message || 'Login failed');
			}
			const data = await response.json();

			if (data.token) {
				authStore.setToken(data.token);
				goto('/dashboard');
			} else {
				throw new Error('No token from server');
			}
		} catch (error) {
			console.error(error);
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
			<input type="checkbox" id="remember" name="remember" />
			<label for="remember">Remember me</label>
		</div>
		<button type="submit" disabled={loading}>{loading ? 'Logging in...' : 'Login'}</button>
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
	button {
		margin: 10px;
		padding: 5px;
		width: 200px;
	}
	input {
		margin: 10px;
		padding: 5px;
		width: 200px;
	}

	button {
		margin: 10px;
		padding: 5px;
		width: 200px;
	}
</style>
