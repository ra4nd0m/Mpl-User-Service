<script lang="ts">
	import { registerUser, SubscriptionType, type NewUser } from '$lib/api/adminClient';

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
				<h3>Register New User</h3>
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
						<h4>Account Details</h4>
						<div class="form-group">
							<label for="email">Email</label>
							<input type="email" id="email" bind:value={newUser.email} required />
						</div>

						<div class="form-group">
							<label for="password">Password</label>
							<input type="password" id="password" bind:value={newUser.password} required />
						</div>

						<div class="form-group">
							<label for="confirm-password">Confirm Password</label>
							<input type="password" id="confirm-password" bind:value={confirmPassword} required />
						</div>
					</div>

					<div class="form-section">
						<h4>Organization Details</h4>
						<div class="form-group">
							<label for="org-name">Organization Name</label>
							<input type="text" id="org-name" bind:value={newUser.organization!.name} required />
						</div>

						<div class="form-group">
							<label for="inn">INN</label>
							<input type="text" id="inn" bind:value={newUser.organization!.inn} required />
						</div>

						<div class="form-group">
							<label for="subscription-type">Subscription Type</label>
							<select id="subscription-type" bind:value={newUser.organization!.subscriptionType}>
								<option value={SubscriptionType.Free}>Free</option>
								<option value={SubscriptionType.Basic}>Basic</option>
								<option value={SubscriptionType.Premium}>Premium</option>
							</select>
						</div>

						<div class="form-row">
							<div class="form-group half">
								<label for="start-date">Start Date</label>
								<input
									type="date"
									id="start-date"
									bind:value={newUser.organization!.subscriptionStartDate}
								/>
							</div>

							<div class="form-group half">
								<label for="end-date">End Date</label>
								<input
									type="date"
									id="end-date"
									bind:value={newUser.organization!.subscriptionEndDate}
								/>
							</div>
						</div>
					</div>

					<div class="form-actions">
						<button type="button" class="cancel-button" onclick={closeModal}>Cancel</button>
						<button type="submit" class="submit-button" disabled={formSubmitting}>
							{formSubmitting ? 'Registering...' : 'Register User'}
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
