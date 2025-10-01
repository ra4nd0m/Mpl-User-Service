<script lang="ts">
	let {
		title,
		size = { width: '900px', height: '90vh' },
		Component,
		componentProps = {},
		showModal = $bindable()
	}: ModalBaseProps = $props();

	function closeModal() {
		showModal = false;
	}

	function handleBackdropClick(event: MouseEvent) {
		if (event.target === event.currentTarget) {
			closeModal();
		}
	}

	function handleEscKey(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			closeModal();
		}
	}

	$effect(() => {
		if (showModal) {
			document.addEventListener('keydown', handleEscKey);
			return () => {
				document.removeEventListener('keydown', handleEscKey);
			};
		}
	});

	type ModalBaseProps = {
		showModal: boolean;
		title: string;
		size?: ModalWindowSizes;
		Component: any;
		componentProps?: any;
	};

	type ModalWindowSizes = {
		width: string;
		height: string;
	};
</script>

{#if showModal}
	<div class="modal-backdrop" onclick={handleBackdropClick} role="presentation">
		<div
			class="modal-container"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: {size?.width}; max-height: {size?.height};"
		>
			<div class="modal-header">
				<h3>{title}</h3>
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
				<Component {...componentProps} />
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

	.modal-body {
		flex: 1;
		overflow: auto;
		padding: 5px;
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
</style>
