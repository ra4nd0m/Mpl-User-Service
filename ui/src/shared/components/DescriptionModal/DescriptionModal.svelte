<script lang="ts">
	import ModalBase from '$components/ModalBase/ModalBase.svelte';
	import DescriptionModalContent from '$components/DescriptionModal/DescriptionModalContent.svelte';
	import { m } from '$lib/i18n';

	let {
		showModal = $bindable(),
		title = m.materials_description_header(),
		description,
		onEditFinish
	}: DescriptionModalProps = $props();

	type DescriptionModalProps = {
		showModal: boolean;
		title?: string;
		description: string;
		onEditFinish: (description: string) => Promise<void> | void;
	};
</script>

<ModalBase
	bind:showModal={showModal}
	{title}
	Component={DescriptionModalContent}
	componentProps={{ description, onEditFinish }}
	size={{ width: '760px', height: '70vh' }}
/>
