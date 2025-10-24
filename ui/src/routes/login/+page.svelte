<script lang="ts">
	import { goto } from '$app/navigation';
	import { authStore } from '$lib/stores/authStore';
	import { login } from '$lib/api/authClient';
	import { locales, switchLocale, locale, m } from '$lib/i18n';

	let email = $state('');
	let password = $state('');
	let rememberMe = $state(false);
	let error = $state('');
	let loading = $state(false);
	let showPassword = $state(false);

	function togglePasswordVisibility() {
		showPassword = !showPassword;
	}

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
				error = result.error;
			}
		} catch (err) {
			console.error(err);
			error = 'An error occurred';
		} finally {
			loading = false;
			authStore.setLoading(false);
		}
	}

	function handleLanguageChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		switchLocale(target.value as 'en' | 'ru');
	}
</script>

<svelte:head>
	{#key $locale}
		<title>{m.login_header()}</title>
		<meta name="description" content={m.login_meta()} />
	{/key}
</svelte:head>

{#key $locale}
	<div class="login-container">
		<div class="language-toggle">
			<select value={$locale} onchange={handleLanguageChange}>
				{#each locales as lang}
					<option value={lang}>{lang.toUpperCase()}</option>
				{/each}
			</select>
		</div>
		<form onsubmit={handleLogin}>
			<div class="logo">
				<svg
					width="235"
					height="25"
					viewBox="0 0 235 25"
					fill="none"
					xmlns="http://www.w3.org/2000/svg"
				>
					<path
						d="M0 0.489445V25H5.32212V7.5715L10.1066 25H14.8374L19.4069 7.51617V25H26.2343V0.378788H16.5577L12.7946 12.6617L9.40779 0.378788L0 0.489445Z"
						fill="#757575"
					></path>
					<path
						d="M30.6682 0.378788V25H48.4041V18.9692H37.9214V14.9855H46.1276V9.78464H37.8155V5.74566H48.4041V0.378788H30.6682Z"
						fill="#757575"
					></path>
					<path
						d="M217.264 0.378788V25H235V18.9692H224.517V14.9855H232.723V9.78464H224.411V5.74566H235V0.378788H217.264Z"
						fill="#EA5B21"
					></path>
					<path
						d="M49.5126 0.378788V5.85632H54.278V25H61.6381V5.85632H66.5094V0.378788H49.5126Z"
						fill="#757575"
					></path>
					<path
						d="M80.165 0.378788H72.3299L65.0314 25H70.9883L72.3299 19.9651H78.7697L80.2187 25H87.5708L80.165 0.378788ZM73.4032 14.9302L75.4961 7.62683L77.7501 14.9302H73.4032Z"
						fill="#757575"
					></path>
					<path
						d="M185.102 0.378788H177.267L169.969 25H175.925L177.267 19.9651H183.707L185.156 25H192.508L185.102 0.378788ZM178.394 14.9302L180.487 7.62683L182.741 14.9302H178.394Z"
						fill="#EA5B21"
					></path>
					<path d="M89.7877 0.378788V25H106.415V18.9692H97.0521V0.378788H89.7877Z" fill="#757575"
					></path>
					<path d="M109.371 0.378788V25H125.629V18.9692H116.474V0.378788H109.371Z" fill="#757575"
					></path>
					<path d="M151.494 0.378788V25H167.752V18.9692H158.567V0.378788H151.494Z" fill="#EA5B21"
					></path>
					<path
						d="M141.591 0.434118L128.585 0.378788V25H135.738V15.8708H142.458C142.458 15.8708 148.907 14.7089 148.907 7.90347C148.961 1.15339 141.591 0.434118 141.591 0.434118ZM139.64 10.6146H135.792V5.52435H139.423C139.423 5.52435 141.537 5.74566 141.537 8.06946C141.537 10.3933 139.64 10.6146 139.64 10.6146Z"
						fill="#EA5B21"
					></path>
					<path
						d="M207.999 9.66939H214.292C214.292 9.66939 214.345 -0.162736 204.668 0.00204841C194.991 0.166833 193.616 9.61446 193.616 12.251C193.616 14.8876 194.092 24.7197 204.615 24.9943C215.138 25.269 214.292 15.4918 214.292 15.4918H207.999C207.999 15.4918 207.365 19.7212 204.562 19.7212C199.592 19.5015 199.327 5.385 204.827 5.385C208.211 5.385 207.999 9.66939 207.999 9.66939Z"
						fill="#EA5B21"
					></path>
				</svg>
			</div>
			<h2 class="form-title">{m.login_header()}</h2>
			<input type="text" placeholder={m.login_email()} bind:value={email} required />
			<div class="password-input-container">
				<input
					type={showPassword ? 'text' : 'password'}
					placeholder={m.login_password()}
					bind:value={password}
					required
				/>
				<button
					type="button"
					class="password-toggle-button"
					onclick={togglePasswordVisibility}
					title={showPassword ? m.universal_hide_password() : m.universal_show_password()}
					aria-label={showPassword ? m.universal_hide_password() : m.universal_show_password()}
				>
					{#if showPassword}
						<svg
							xmlns="http://www.w3.org/2000/svg"
							width="16"
							height="16"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2"
							stroke-linecap="round"
							stroke-linejoin="round"
						>
							<path d="m15 18-.722-3.25"></path>
							<path d="M2 8a10.645 10.645 0 0 0 20 0"></path>
							<path d="m20 15-1.726-2.05"></path>
							<path d="m4 15 1.726-2.05"></path>
							<path d="m9 18 .722-3.25"></path>
						</svg>
					{:else}
						<svg
							xmlns="http://www.w3.org/2000/svg"
							width="16"
							height="16"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							stroke-width="2"
							stroke-linecap="round"
							stroke-linejoin="round"
						>
							<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
							<circle cx="12" cy="12" r="3"></circle>
						</svg>
					{/if}
				</button>
			</div>
			<div class="remember-container">
				<input type="checkbox" id="remember" bind:checked={rememberMe} />
				<label for="remember">{m.login_remember_me()}</label>
			</div>
			{#if error}
				<div class="error-message">{error}</div>
			{/if}
			<button type="submit" disabled={loading}
				>{loading ? m.login_process() : m.login_button()}</button
			>
		</form>
	</div>
{/key}

<style>
	.login-container {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 75vh;
		position: relative;
	}

	.language-toggle {
		position: absolute;
		top: 20px;
		right: 20px;
		z-index: 10;
	}

	.language-toggle select {
		padding: 8px 12px;
		border: 1px solid #ced4da;
		border-radius: 4px;
		background-color: white;
		color: #727271;
		font-size: 14px;
		cursor: pointer;
		transition: border-color 0.2s;
	}

	.language-toggle select:focus {
		outline: none;
		border-color: #ea5b21;
		box-shadow: 0 0 0 3px rgba(234, 91, 33, 0.1);
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
		background-color: white;
	}

	.logo {
		display: flex;
		align-items: center;
		margin-bottom: 20px;
	}

	.logo svg {
		height: 30px;
		width: auto;
	}

	.form-title {
		margin-bottom: 20px;
		color: #727271;
		font-size: 1.5rem;
	}

	input[type='text'],
	input[type='password'] {
		margin: 10px;
		padding: 12px 16px;
		width: 200px;
		border: 1px solid #ced4da;
		border-radius: 4px;
		font-size: 14px;
		color: #727271;
		background-color: white;
		transition:
			border-color 0.2s,
			box-shadow 0.2s;
		box-sizing: border-box;
	}

	input[type='text']:focus,
	input[type='password']:focus {
		outline: none;
		border-color: #ea5b21;
		box-shadow: 0 0 0 3px rgba(234, 91, 33, 0.1);
	}

	.password-input-container {
		position: relative;
		margin: 10px;
		width: 200px;
	}

	.password-input-container input[type='password'],
	.password-input-container input[type='text'] {
		margin: 0;
		width: 100%;
		padding: 12px 40px 12px 16px;
		border: 1px solid #ced4da;
		border-radius: 4px;
		font-size: 14px;
		color: #727271;
		background-color: white;
		transition:
			border-color 0.2s,
			box-shadow 0.2s;
		box-sizing: border-box;
	}

	.password-toggle-button {
		position: absolute;
		right: 12px;
		top: 50%;
		transform: translateY(-50%);
		background: none;
		border: none;
		cursor: pointer;
		padding: 4px;
		margin: 0;
		width: 24px;
		height: 24px;
		color: #727271;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 4px;
		transition:
			color 0.2s,
			background-color 0.2s;
	}

	.password-toggle-button:hover {
		background-color: rgba(234, 91, 33, 0.1);
		color: #ea5b21;
	}

	.password-toggle-button:focus {
		outline: 2px solid #ea5b21;
		outline-offset: 1px;
	}

	.password-toggle-button:disabled {
		background-color: transparent;
		cursor: not-allowed;
		opacity: 0.5;
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

	.remember-container label {
		color: #727271;
		font-size: 14px;
	}

	.error-message {
		color: #e74c3c;
		margin: 10px 0;
		text-align: center;
		width: 200px;
		font-size: 14px;
		background-color: rgba(231, 76, 60, 0.1);
		padding: 8px;
		border-radius: 4px;
	}

	button[type='submit'] {
		margin: 10px;
		padding: 12px 16px;
		width: 200px;
		background-color: #ea5b21;
		color: white;
		border: none;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
		font-size: 14px;
		font-weight: 500;
	}

	button[type='submit']:hover:not(:disabled) {
		background-color: #d54e1a;
	}

	button[type='submit']:disabled {
		background-color: rgba(234, 91, 33, 0.5);
		cursor: not-allowed;
	}

	@media (max-width: 600px) {
		.logo svg {
			height: 25px;
		}

		form {
			width: 90%;
			max-width: 300px;
		}
	}
</style>
