<script lang="ts">
	import { authStore } from '$lib/stores/authStore';

	let { description, onEditFinish }: DescriptionModalContentProps = $props();

	let editableDescription = $state(description ?? '');
	let isEditing = $state(false);
	let isSaving = $state(false);
	let errorMessage = $state<string | null>(null);

	const isAdmin = $derived($authStore.roles?.includes('Admin'));
	const hasDescription = $derived((description ?? '').trim().length > 0);
	const hasChanges = $derived(editableDescription !== (description ?? ''));

	$effect(() => {
		if (!isEditing) {
			editableDescription = description ?? '';
			errorMessage = null;
		}
	});

	async function saveDescription() {
		if (!hasChanges || isSaving) {
			return;
		}

		errorMessage = null;
		isSaving = true;

		try {
			await onEditFinish(editableDescription);
			isEditing = false;
		} catch (error) {
			errorMessage = error instanceof Error ? error.message : 'Failed to update description';
		} finally {
			isSaving = false;
		}
	}

	function cancelEdit() {
		editableDescription = description ?? '';
		errorMessage = null;
		isEditing = false;
	}

	type DescriptionModalContentProps = {
		description: string;
		onEditFinish: (description: string) => Promise<void> | void;
	};
</script>

<div class="description-modal-content">
	{#if isEditing}
		<textarea
			bind:value={editableDescription}
			class="description-editor"
			rows="10"
			placeholder="Enter description"
			aria-label="Description"
		></textarea>

		{#if errorMessage}
			<p class="error-message">{errorMessage}</p>
		{/if}

		<div class="actions">
			<button type="button" class="btn btn-secondary" onclick={cancelEdit} disabled={isSaving}>Cancel</button>
			<button
				type="button"
				class="btn btn-primary"
				onclick={saveDescription}
				disabled={!hasChanges || isSaving}
			>
				{isSaving ? 'Saving...' : 'Save'}
			</button>
		</div>
	{:else}
		<div class="description-text-wrap">
			{#if hasDescription}
				<p class="description-text">{description}</p>
			{:else}
				<p class="description-empty">No description provided.</p>
			{/if}
		</div>

		{#if isAdmin}
			<div class="actions">
				<button type="button" class="btn btn-primary" onclick={() => (isEditing = true)}>Edit</button>
			</div>
		{/if}
	{/if}
</div>

<style>
	.description-modal-content {
		display: flex;
		flex-direction: column;
		gap: 1rem;
		padding: 1rem;
	}

	.description-text-wrap {
		max-height: 50vh;
		overflow-y: auto;
	}

	.description-text {
		margin: 0;
		color: #343a40;
		line-height: 1.6;
		white-space: pre-wrap;
		word-break: break-word;
	}

	.description-empty {
		margin: 0;
		color: #6c757d;
		font-style: italic;
	}

	.description-editor {
		width: 100%;
		min-height: 220px;
		padding: 0.75rem;
		border: 1px solid #ced4da;
		border-radius: 6px;
		font: inherit;
		line-height: 1.5;
		resize: vertical;
		box-sizing: border-box;
	}

	.description-editor:focus {
		outline: none;
		border-color: #ea5b21;
		box-shadow: 0 0 0 2px rgba(234, 91, 33, 0.15);
	}

	.actions {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
	}

	.btn {
		border: none;
		border-radius: 6px;
		padding: 0.55rem 1rem;
		font-size: 0.875rem;
		font-weight: 600;
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.btn:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}

	.btn-secondary {
		background-color: #6c757d;
		color: #fff;
	}

	.btn-secondary:hover:not(:disabled) {
		background-color: #5a6268;
	}

	.btn-primary {
		background-color: #ea5b21;
		color: #fff;
	}

	.btn-primary:hover:not(:disabled) {
		background-color: #d24d1a;
	}

	.error-message {
		margin: 0;
		color: #dc3545;
		font-size: 0.875rem;
	}
</style>
