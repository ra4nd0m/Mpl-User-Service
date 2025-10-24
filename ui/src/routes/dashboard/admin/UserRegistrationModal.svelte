<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import {
		registerUser,
		SubscriptionType,
		updateUser,
		getOrgs,
		type NewUser,
		type UpdatedUser,
		type UserResponse
	} from '$lib/api/adminClient';

	import type { OrgResponse } from '$lib/api/adminClient';

	import { m, locale } from '$lib/i18n';
	import { onMount } from 'svelte';
	import OrgRegistrationModal from './OrgRegistrationModal.svelte';

	let {
		onUserAdded,
		mode = 'create',
		existingUser = null
	}: {
		onUserAdded: () => void;
		mode?: 'create' | 'edit';
		existingUser?: UserResponse | null;
	} = $props();

	let newUser: NewUser = $state(
		existingUser
			? {
					email: existingUser.email,
					password: '', // Don't populate password for edit mode
					organization: existingUser.org
						? {
								name: existingUser.org.name,
								inn: existingUser.org.inn,
								subscriptionType: existingUser.org.subscriptionType,
								subscriptionStartDate: existingUser.org.subscriptionStartDate,
								subscriptionEndDate: existingUser.org.subscriptionEndDate
							}
						: {
								name: '',
								inn: '',
								subscriptionType: SubscriptionType.Free,
								subscriptionStartDate: new Date().toISOString().split('T')[0],
								subscriptionEndDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000)
									.toISOString()
									.split('T')[0]
							}
				}
			: {
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
				}
	);

	let confirmPassword = $state('');
	let formError: string | null = $state(null);
	let formSuccess: string | null = $state(null);
	let formSubmitting = $state(false);
	let showPassword = $state(false);

	let organizations = $state<OrgResponse[]>([]);
	let selectedOrgId = $state<string | null>(null);
	let loadingOrgs = $state(false);

	let isCreateOrgModalOpen = $state(false);

	onMount(async () => {
		await loadOrgs();
	});

	async function handleOrgAdded() {
		isCreateOrgModalOpen = false;
		await loadOrgs();
	}

	function handleCreateOrgModal() {
		isCreateOrgModalOpen = true;
	}

	async function loadOrgs() {
		try {
			loadingOrgs = true;
			const orgs = await getOrgs();
			organizations = orgs || [];

			// If editing and user has an org, pre-select it
			if (mode === 'edit' && existingUser?.org) {
				const matchingOrg = organizations.find((org) => org.inn === existingUser.org?.inn);
				if (matchingOrg) {
					selectedOrgId = matchingOrg.inn;
				}
			}
		} catch (err) {
			console.error('Failed to load organizations', err);
			formError = 'Failed to load organizations';
		} finally {
			loadingOrgs = false;
		}
	}

	function getSubscriptionTypeName(type: SubscriptionType): string {
		switch (type) {
			case SubscriptionType.Free:
				return m.admin_create_user_subscription_type_free();
			case SubscriptionType.Basic:
				return m.admin_create_user_subscription_type_basic();
			case SubscriptionType.Premium:
				return m.admin_create_user_subscription_type_premium();
			default:
				return 'Unknown';
		}
	}

	function handleOrgSelection(e: Event) {
		const select = e.target as HTMLSelectElement;
		const orgId = select.value;
		selectedOrgId = orgId;

		const selectedOrg = organizations.find((org) => org.inn === orgId);
		if (selectedOrg) {
			newUser.organization = {
				name: selectedOrg.name,
				inn: selectedOrg.inn,
				subscriptionType: selectedOrg.subscriptionType,
				subscriptionStartDate: selectedOrg.subscriptionStartDate,
				subscriptionEndDate: selectedOrg.subscriptionEndDate
			};
		}
	}

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

	function validatePassword(password: string): { valid: boolean; error?: string } {
		if (!password || password.length < 8) {
			return { valid: false, error: m.admin_create_user_error_password_too_short() };
		}

		const hasLowercase = /[a-z]/.test(password);
		const hasUppercase = /[A-Z]/.test(password);
		const hasDigit = /[0-9]/.test(password);
		const hasSpecialChar = /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(password);

		if (!hasLowercase) {
			return { valid: false, error: m.admin_create_user_error_password_no_lowercase() };
		}
		if (!hasUppercase) {
			return { valid: false, error: m.admin_create_user_error_password_no_uppercase() };
		}
		if (!hasDigit) {
			return { valid: false, error: m.admin_create_user_error_password_no_number() };
		}
		if (!hasSpecialChar) {
			return { valid: false, error: m.admin_create_user_error_password_no_special() };
		}

		return { valid: true };
	}

	function handleGeneratePassword() {
		const generatedPassword = generateSecurePassword(12);
		newUser.password = generatedPassword;
		confirmPassword = generatedPassword;
	}

	function togglePasswordVisibility() {
		showPassword = !showPassword;
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

		if (!newUser.email) {
			formError = 'Email is required';
			return;
		}

		// Only validate password for create mode or if password is being changed
		if (mode === 'create' || newUser.password) {
			if (!newUser.password) {
				formError = 'Password is required';
				return;
			}

			const passwordValidation = validatePassword(newUser.password);
			if (!passwordValidation.valid) {
				formError = passwordValidation.error || 'Invalid password';
				return;
			}

			if (newUser.password !== confirmPassword) {
				formError = 'Passwords do not match';
				return;
			}
		}

		if (!selectedOrgId) {
			formError = 'Organization selection is required';
			return;
		}

		try {
			formSubmitting = true;

			if (mode === 'edit' && existingUser) {
				// Prepare UpdatedUser data
				const updateData: UpdatedUser = {
					// Only include newEmail if email has changed
					newEmail: newUser.email !== existingUser.email ? newUser.email : undefined,
					// Only include password if it's being changed
					password: newUser.password || undefined,
					// Always include organization
					organization: newUser.organization
				};

				// Call update API - use EXISTING email to identify the user
				const result = await updateUser(existingUser.email, updateData);

				if (result) {
					formSuccess = `User ${existingUser.email} updated successfully`;
					resetForm();
					if (onUserAdded) onUserAdded();
				} else {
					formError = 'Failed to update user';
				}
			} else {
				// Call register API - NewUser expects "password" and "organization"
				const result = await registerUser(newUser);

				if (result) {
					formSuccess = `User ${result.email} registered successfully`;
					resetForm();
					if (onUserAdded) onUserAdded();
				} else {
					formError = 'Failed to register user';
				}
			}
		} catch (err) {
			console.error(mode === 'edit' ? 'Failed to update user' : 'Failed to register user', err);
			formError = mode === 'edit' ? 'Failed to update user' : 'Failed to register user';
		} finally {
			formSubmitting = false;
		}
	}
</script>

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
				{#if mode === 'edit'}
					<small class="password-requirements">
						Original email: {existingUser?.email}
					</small>
				{/if}
			</div>

			<div class="form-group">
				<label for="password">
					{mode === 'edit' ? 'New Password (optional)' : m.admin_create_user_password()}
				</label>
				<div class="password-input-group">
					<input
						type={showPassword ? 'text' : 'password'}
						id="password"
						bind:value={newUser.password}
						required={mode === 'create'}
						placeholder={mode === 'edit' ? 'Leave blank to keep current password' : ''}
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
					{mode === 'edit'
						? 'Leave blank to keep current password. If changing: ' +
							m.admin_create_user_password_requirements()
						: m.admin_create_user_password_requirements()}
				</small>
			</div>

			{#if mode === 'create' || newUser.password}
				<div class="form-group">
					<label for="confirm-password">{m.admin_create_user_confirm_password()}</label>
					<input
						type={showPassword ? 'text' : 'password'}
						id="confirm-password"
						bind:value={confirmPassword}
						required={mode === 'create' || !!newUser.password}
					/>
				</div>
			{/if}
		</div>

		<div class="form-group">
			<label for="org-select">Select Organization</label>
			{#if loadingOrgs}
				<p>Loading organizations...</p>
			{:else}
				<select id="org-select" bind:value={selectedOrgId} onchange={handleOrgSelection} required>
					<option value="">-- Select an organization --</option>
					{#each organizations as org}
						<option value={org.inn}>
							{org.name} (INN: {org.inn})
						</option>
					{/each}
				</select>
				<button type="button" class="create-org-button" onclick={handleCreateOrgModal}>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						width="14"
						height="14"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
					>
						<path d="M5 12h14"></path>
						<path d="M12 5v14"></path>
					</svg>
					Create Organisation
				</button>
			{/if}
			{#if selectedOrgId && newUser.organization}
				<div class="org-details">
					<p><strong>Organization Name:</strong> {newUser.organization.name}</p>
					<p><strong>INN:</strong> {newUser.organization.inn}</p>
					<p>
						<strong>Subscription Type:</strong>
						{getSubscriptionTypeName(newUser.organization.subscriptionType)}
					</p>
					<p>
						<strong>Subscription Period:</strong>
						{new Intl.DateTimeFormat($locale).format(
							new Date(newUser.organization.subscriptionStartDate)
						)} to {new Intl.DateTimeFormat($locale).format(
							new Date(newUser.organization.subscriptionEndDate)
						)}
					</p>
				</div>
			{/if}
		</div>

		<div class="form-actions">
			<button type="button" class="cancel-button" onclick={resetForm}
				>{m.admin_create_user_reset_button()}</button
			>
			<button type="submit" class="submit-button" disabled={formSubmitting}>
				{formSubmitting
					? mode === 'edit'
						? 'Updating...'
						: m.admin_create_user_registering()
					: mode === 'edit'
						? 'Update User'
						: m.admin_create_user_register_button()}
			</button>
		</div>
	</form>
</div>

<ModalBase
	bind:showModal={isCreateOrgModalOpen}
	title="Create Organization"
	Component={OrgRegistrationModal}
	componentProps={{ onOrgAdded: handleOrgAdded }}
/>

<style>
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

	.create-org-button {
		display: inline-flex;
		align-items: center;
		gap: 0.25rem;
		margin-top: 0.5rem;
		padding: 0.25rem 0.5rem;
		background: none;
		border: none;
		color: #228be6;
		font-size: 0.875rem;
		cursor: pointer;
		transition: color 0.2s;
	}

	.create-org-button:hover {
		color: #1c7ed6;
	}

	.create-org-button svg {
		flex-shrink: 0;
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
</style>
