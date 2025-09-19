<script lang="ts">
	import { registerUser, SubscriptionType, type NewUser } from '$lib/api/adminClient';

	import { m } from '$lib/i18n';

	let { showModal = $bindable(), onUserAdded = $bindable() } = $props();

	let newUser: NewUser = $state({
		email: '',
		password: '',
		organization: {
			name: '',
			inn: '',
			subscriptionType: SubscriptionType.Free,
			subscriptionStartDate: new Date().toISOString().split('T')[0],
			subscriptionEndDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000)
				.toISOString()
				.split('T')[0]
		}
	});

	let confirmPassword = $state('');
	let formError: string | null = $state(null);
	let formSuccess: string | null = $state(null);
	let formSubmitting = $state(false);
	let showPassword = $state(false);

	function generateSecurePassword(length: number = 12): string {
		const lowercase = 'abcdefghijklmnopqrstuvwxyz';
		const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
		const digits = '0123456789';
		const symbols = '!@#$%^&*()_+-=[]{}|;:,.<>?';

		let password = '';
		password += lowercase[Math.floor(Math.random() * lowercase.length)];
		password += uppercase[Math.floor(Math.random() * uppercase.length)];
		password += digits[Math.floor(Math.random() * digits.length)];
		password += symbols[Math.floor(Math.random() * symbols.length)];

		const allCharacters = lowercase + uppercase + digits + symbols;
		for (let i = 4; i < length; i++) {
			password += allCharacters[Math.floor(Math.random() * allCharacters.length)];
		}

		return password;
	}

	function handleGeneratePassword() {
		const generatedPassword = generateSecurePassword(12);
		newUser.password = generatedPassword;
		confirmPassword = generatedPassword;
	}

	function togglePasswordVisibility() {
		showPassword = !showPassword;
	}

	function closeModal() {
		showModal = false;
		resetForm();
	}

	function handleBackdropClick(e: MouseEvent) {
		if (e.target === e.currentTarget) {
			closeModal();
		}
	}

	function resetForm() {
		newUser = {
			email: '',
			password: '',
			organization: {
				name: '',
				inn: '',
				subscriptionType: SubscriptionType.Free,
				subscriptionStartDate: new Date().toISOString().split('T')[0],
				subscriptionEndDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000)
					.toISOString()
					.split('T')[0]
			}
		};
		confirmPassword = '';
		formError = null;
		formSuccess = null;
	}

	async function handleRegisterSubmit(e: SubmitEvent) {
		e.preventDefault();

		formError = null;
		formSuccess = null;

		if (!newUser.email || !newUser.password) {
			formError = 'Email and password are required';
			return;
		}
		if (newUser.password !== confirmPassword) {
			formError = 'Passwords do not match';
			return;
		}
		if (!newUser.organization || !newUser.organization.name || !newUser.organization.inn) {
			formError = 'Organization name and INN are required';
			return;
		}

		try {
			formSubmitting = true;
			const result = await registerUser(newUser);

			if (result) {
				formSuccess = `User ${result.email} registered successfully`;
				closeModal();
				if (onUserAdded) onUserAdded();
			} else {
				formError = 'Failed to register user';
			}
		} catch (err) {
			console.error('Failed to register user', err);
			formError = 'Failed to register user';
		} finally {
			formSubmitting = false;
		}
	}
</script>

{#if showModal}
	<div class="modal-backdrop" onclick={handleBackdropClick} role="presentation">
		<div class="modal-container">
			<div class="modal-header">
				<h3>{m.admin_create_user_header()}</h3>
				<button class="close-button" onclick={closeModal} aria-label="Close">
					<svg
						xmlns="http://www.w3.org/2000/svg"
						width="18"
						height="18"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
					>
						<line x1="18" y1="6" x2="6" y2="18"></line>
						<line x1="6" y1="6" x2="18" y2="18"></line>
					</svg>
				</button>
			</div>

			<div class="modal-body">
				{#if formError}
					<div class="form-error">{formError}</div>
				{/if}

				{#if formSuccess}
					<div class="form-success">{formSuccess}</div>
				{/if}

				<form onsubmit={handleRegisterSubmit}>
					<div class="form-section">
						<h4>{m.admin_create_user_account_details()}</h4>
						<div class="form-group">
							<label for="email">{m.admin_create_user_email()}</label>
							<input type="email" id="email" bind:value={newUser.email} required />
						</div>

						<div class="form-group">
							<label for="password">{m.admin_create_user_password()}</label>
							<div class="password-input-group">
								<input
									type={showPassword ? 'text' : 'password'}
									id="password"
									bind:value={newUser.password}
									required
								/>
								<button
									type="button"
									class="password-toggle-button"
									onclick={togglePasswordVisibility}
									title={showPassword
										? m.universal_hide_password()
										: m.universal_show_password()}
									aria-label={showPassword
										? m.universal_hide_password()
										: m.universal_show_password()}
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
								<button
									type="button"
									class="generate-password-button"
									onclick={handleGeneratePassword}
									title={m.admin_create_user_generate_password()}
									aria-label={m.admin_create_user_generate_password()}
								>
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
										<path d="M12 2v4"></path>
										<path d="m16.2 7.8 2.9-2.9"></path>
										<path d="M18 12h4"></path>
										<path d="m16.2 16.2 2.9 2.9"></path>
										<path d="M12 18v4"></path>
										<path d="m4.9 19.1 2.9-2.9"></path>
										<path d="M2 12h4"></path>
										<path d="m4.9 4.9 2.9 2.9"></path>
									</svg>
								</button>
							</div>
							<small class="password-requirements">
								{m.admin_create_user_password_requirements()}
							</small>
						</div>

						<div class="form-group">
							<label for="confirm-password">{m.admin_create_user_confirm_password()}</label>
							<input
								type={showPassword ? 'text' : 'password'}
								id="confirm-password"
								bind:value={confirmPassword}
								required
							/>
						</div>
					</div>

					<div class="form-section">
						<h4>{m.admin_create_user_organization_details()}</h4>
						<div class="form-group">
							<label for="org-name">{m.admin_create_user_organization_name()}</label>
							<input type="text" id="org-name" bind:value={newUser.organization!.name} required />
						</div>

						<div class="form-group">
							<label for="inn">{m.admin_create_user_organization_inn()}</label>
							<input type="text" id="inn" bind:value={newUser.organization!.inn} required />
						</div>

						<div class="form-group">
							<label for="subscription-type">{m.admin_create_user_subscription_type()}</label>
							<select id="subscription-type" bind:value={newUser.organization!.subscriptionType}>
								<option value={SubscriptionType.Free}>{m.admin_create_user_subscription_type_free()}</option>
								<option value={SubscriptionType.Basic}>{m.admin_create_user_subscription_type_basic()}</option>
								<option value={SubscriptionType.Premium}>{m.admin_create_user_subscription_type_premium()}</option>
							</select>
						</div>

						<div class="form-row">
							<div class="form-group half">
								<label for="start-date">{m.admin_create_user_subscription_start_date()}</label>
								<input
									type="date"
									id="start-date"
									bind:value={newUser.organization!.subscriptionStartDate}
								/>
							</div>

							<div class="form-group half">
								<label for="end-date">{m.admin_create_user_subscription_end_date()}</label>
								<input
									type="date"
									id="end-date"
									bind:value={newUser.organization!.subscriptionEndDate}
								/>
							</div>
						</div>
					</div>

					<div class="form-actions">
						<button type="button" class="cancel-button" onclick={closeModal}>{m.admin_create_user_cancel_button()}</button>
						<button type="submit" class="submit-button" disabled={formSubmitting}>
							{formSubmitting ? m.admin_create_user_registering() : m.admin_create_user_register_button()}
						</button>
					</div>
				</form>
			</div>
		</div>
	</div>
{/if}

<style>
	.modal-backdrop {
		position: fixed;
		top: 0;
		left: 0;
		width: 100%;
		height: 100%;
		background-color: rgba(0, 0, 0, 0.5);
		display: flex;
		justify-content: center;
		align-items: center;
		z-index: 100;
	}

	.modal-container {
		background-color: white;
		border-radius: 8px;
		width: 90%;
		max-width: 600px;
		max-height: 90vh;
		overflow-y: auto;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
	}

	.modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 1rem;
		border-bottom: 1px solid #e9ecef;
	}

	.modal-header h3 {
		margin: 0;
		font-size: 1.25rem;
		color: #343a40;
	}

	.close-button {
		background: none;
		border: none;
		cursor: pointer;
		color: #6c757d;
		padding: 0.25rem;
		border-radius: 4px;
		transition: background-color 0.2s;
	}

	.close-button:hover {
		background-color: #f8f9fa;
	}

	.modal-body {
		padding: 1rem;
	}

	.form-section {
		margin-bottom: 1.5rem;
		padding-bottom: 1rem;
		border-bottom: 1px solid #e9ecef;
	}

	.form-section h4 {
		margin-top: 0;
		margin-bottom: 1rem;
		color: #495057;
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-row {
		display: flex;
		gap: 1rem;
	}

	.form-group.half {
		flex: 1;
	}

	label {
		display: block;
		margin-bottom: 0.5rem;
		font-weight: 500;
		color: #495057;
	}

	input,
	select {
		width: 100%;
		padding: 0.5rem;
		border: 1px solid #ced4da;
		border-radius: 4px;
		font-size: 1rem;
		transition: border-color 0.2s;
	}

	input:focus,
	select:focus {
		outline: none;
		border-color: #4dabf7;
		box-shadow: 0 0 0 3px rgba(77, 171, 247, 0.2);
	}

	.password-input-group {
		position: relative;
		display: flex;
		align-items: center;
	}

	.password-input-group input {
		padding-right: 5rem;
	}

	.password-input-group {
		position: relative;
		display: flex;
		align-items: center;
	}

	.password-input-group input {
		padding-right: 5rem;
	}

	.password-toggle-button {
		position: absolute;
		right: 2.5rem;
		background: none;
		border: none;
		cursor: pointer;
		color: #6c757d;
		padding: 0.25rem;
		border-radius: 4px;
		transition:
			background-color 0.2s,
			color 0.2s;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.password-toggle-button:hover {
		background-color: #f8f9fa;
		color: #495057;
	}

	.generate-password-button {
		position: absolute;
		right: 0.5rem;
		background: none;
		border: none;
		cursor: pointer;
		color: #6c757d;
		padding: 0.25rem;
		border-radius: 4px;
		transition:
			background-color 0.2s,
			color 0.2s;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.generate-password-button:hover {
		background-color: #f8f9fa;
		color: #495057;
	}

	.password-requirements {
		display: block;
		margin-top: 0.25rem;
		font-size: 0.875rem;
		color: #6c757d;
		font-style: italic;
	}

	.form-actions {
		display: flex;
		justify-content: flex-end;
		gap: 1rem;
		margin-top: 1rem;
	}

	.cancel-button {
		padding: 0.5rem 1rem;
		border: 1px solid #ced4da;
		background-color: white;
		color: #495057;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.cancel-button:hover {
		background-color: #f8f9fa;
	}

	.submit-button {
		padding: 0.5rem 1rem;
		border: none;
		background-color: #228be6;
		color: white;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.submit-button:hover {
		background-color: #1c7ed6;
	}

	.submit-button:disabled {
		background-color: #74c0fc;
		cursor: not-allowed;
	}

	.form-error {
		background-color: rgba(220, 53, 69, 0.1);
		color: #dc3545;
		padding: 0.75rem;
		border-radius: 4px;
		margin-bottom: 1rem;
	}

	.form-success {
		background-color: rgba(40, 167, 69, 0.1);
		color: #28a745;
		padding: 0.75rem;
		border-radius: 4px;
		margin-bottom: 1rem;
	}

	@media (max-width: 768px) {
		.form-row {
			flex-direction: column;
			gap: 0;
		}

		.modal-container {
			width: 95%;
			max-height: 95vh;
		}
	}
</style>
