<script lang="ts">
	import { createOrg, SubscriptionType, updateOrg, type OrgResponse } from '$lib/api/adminClient';
	import { m } from '$lib/i18n';

	let formError: string | null = $state(null);
	let formSuccess: string | null = $state(null);
	let formSubmitting = $state(false);

	function formatDateForInput(isoString: string): string {
		const date = new Date(isoString);
		// Get the date components in UTC to avoid timezone shifts
		const year = date.getUTCFullYear();
		const month = String(date.getUTCMonth() + 1).padStart(2, '0');
		const day = String(date.getUTCDate()).padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	let {
		onOrgAdded,
		mode = 'create',
		existingOrg
	} = $props<{
		onOrgAdded: () => void;
		mode?: 'create' | 'edit';
		existingOrg?: OrgResponse;
	}>();

	let newOrg: OrgResponse = $state(
		existingOrg && mode === 'edit'
			? {
					id: existingOrg.id,
					name: existingOrg.name,
					inn: existingOrg.inn,
					subscriptionType: existingOrg.subscriptionType,
					subscriptionStartDate: formatDateForInput(existingOrg.subscriptionStartDate),
					subscriptionEndDate: formatDateForInput(existingOrg.subscriptionEndDate)
				}
			: {
					id: 0,
					name: '',
					inn: '',
					subscriptionType: SubscriptionType.Free,
					subscriptionStartDate: '',
					subscriptionEndDate: ''
				}
	);

	function resetForm() {
		newOrg = {
            id: 0,
			name: '',
			inn: '',
			subscriptionType: SubscriptionType.Free,
			subscriptionStartDate: '',
			subscriptionEndDate: ''
		};
		formError = null;
		formSuccess = null;
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		formError = null;
		formSuccess = null;

		if (
			newOrg.subscriptionEndDate &&
			newOrg.subscriptionStartDate &&
			newOrg.subscriptionEndDate < newOrg.subscriptionStartDate
		) {
			formError = 'Subscription end date cannot be earlier than start date.';
			return;
		}

		try {
			formSubmitting = true;
			if (mode === 'edit' && existingOrg) {
				const res = await updateOrg(existingOrg.id, {
					name: newOrg.name,
					inn: newOrg.inn,
					subscriptionType: newOrg.subscriptionType,
					subscriptionStartDate: newOrg.subscriptionStartDate,
					subscriptionEndDate: newOrg.subscriptionEndDate
				});
				if (res) {
					formSuccess = `Organization "${res.name}" updated successfully.`;
					resetForm();
					if (onOrgAdded) {
						onOrgAdded();
					}
				} else {
					formError = 'Failed to create organization.';
				}
			} else {
				const res = await createOrg({
					name: newOrg.name,
					inn: newOrg.inn,
					subscriptionType: newOrg.subscriptionType,
					subscriptionStartDate: newOrg.subscriptionStartDate,
					subscriptionEndDate: newOrg.subscriptionEndDate
				});
				if (res) {
					formSuccess = `Organization "${res.name}" created successfully.`;
					resetForm();
					if (onOrgAdded) {
						onOrgAdded();
					}
				} else {
					formError = 'Failed to create organization.';
				}
			}
		} catch (error) {
			console.error('Failed to create organization:', error);
			formError = 'An error occurred while creating the organization.';
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

	<form onsubmit={handleSubmit}>
		<div class="form-section">
			<h4>{m.admin_create_user_organization_details()}</h4>
			<div class="form-group">
				<label for="org-name">{m.admin_create_user_organization_name()}</label>
				<input type="text" id="org-name" bind:value={newOrg.name} required />
			</div>

			<div class="form-group">
				<label for="inn">{m.admin_create_user_organization_inn()}</label>
				<input type="text" id="inn" bind:value={newOrg.inn} required />
			</div>

			<div class="form-group">
				<label for="subscription-type">{m.admin_create_user_subscription_type()}</label>
				<select id="subscription-type" bind:value={newOrg.subscriptionType} required>
					<option value={SubscriptionType.Free}
						>{m.admin_create_user_subscription_type_free()}</option
					>
					<option value={SubscriptionType.Basic}
						>{m.admin_create_user_subscription_type_basic()}</option
					>
					<option value={SubscriptionType.Premium}
						>{m.admin_create_user_subscription_type_premium()}</option
					>
				</select>
			</div>

			<div class="form-row">
				<div class="form-group half">
					<label for="start-date">{m.admin_create_user_subscription_start_date()}</label>
					<input type="date" id="start-date" bind:value={newOrg.subscriptionStartDate} />
				</div>

				<div class="form-group half">
					<label for="end-date">{m.admin_create_user_subscription_end_date()}</label>
					<input type="date" id="end-date" bind:value={newOrg.subscriptionEndDate} />
				</div>
			</div>
		</div>

		<div class="form-actions">
			<button type="button" class="reset-button" onclick={resetForm}
				>{m.admin_create_user_reset_button()}</button
			>
			<button type="submit" class="submit-button" disabled={formSubmitting}>
				{mode === 'create' ? 'Create' : 'Update'}
			</button>
		</div>
	</form>
</div>

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

	.reset-button {
		padding: 0.5rem 1rem;
		border: 1px solid #ced4da;
		background-color: white;
		color: #495057;
		border-radius: 4px;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.reset-button:hover {
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
	}
</style>
