<script lang="ts">
	let {
		message,
		confirmText = 'Confirm',
		cancelText = 'Cancel',
		onConfirm,
		onCancel
	}: ConfirmationDialogProps = $props();

	type ConfirmationDialogProps = {
		message: string;
		confirmText?: string;
		cancelText?: string;
		onConfirm: () => void;
		onCancel: () => void;
	};
</script>

<div class="confirmation-dialog">
	<p class="message">{message}</p>
	<div class="button-group">
		<button class="btn btn-secondary" onclick={onCancel}>
			{cancelText}
		</button>
		<button class="btn btn-danger" onclick={onConfirm}>
			{confirmText}
		</button>
	</div>
</div>

<style>
	.confirmation-dialog {
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1.5rem;
	}

	.message {
		font-size: 1rem;
		color: #212529;
		margin: 0;
		line-height: 1.5;
	}

	.button-group {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
	}

	.btn {
		padding: 0.5rem 1.5rem;
		border: none;
		border-radius: 4px;
		font-size: 0.875rem;
		font-weight: 500;
		cursor: pointer;
		transition: all 0.2s;
	}

	.btn-secondary {
		background-color: #6c757d;
		color: white;
	}

	.btn-secondary:hover {
		background-color: #5a6268;
	}

	.btn-danger {
		background-color: #dc3545;
		color: white;
	}

	.btn-danger:hover {
		background-color: #c82333;
	}

	.btn:active {
		transform: scale(0.98);
	}
</style>
