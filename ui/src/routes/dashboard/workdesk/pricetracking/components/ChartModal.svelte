<script lang="ts">
	import type { Material, MaterialDateMetricsResp } from '$lib/api/userClient';
	import Chart from 'chart.js/auto';
	import zoomPlugin from 'chartjs-plugin-zoom';
	import { onDestroy, onMount } from 'svelte';

	Chart.register(zoomPlugin);

	const { priceData, materialInfo } = $props();

	let canvas: HTMLCanvasElement | null = $state(null);
	let chart: Chart | null = null;
	let showModal = $state(false);

	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString('ru-RU', {
			month: 'short',
			day: 'numeric'
		});
	}

	function open() {
		showModal = true;
	}

	function close() {
		if (chart) {
			chart.destroy();
			chart = null;
		}
		showModal = false;
	}
	function createChart() {
		if (chart) {
			chart.destroy();
			chart = null;
		}
		if (!canvas || !priceData || priceData.length === 0) return;

		// Sort data by date ascending for proper timeline
		const sortedData = [...priceData].sort(
			(a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
		);

		// Prepare datasets
		const labels = sortedData.map((item) => formatDate(item.date));

		const datasets = [];

		// Only add datasets for props that exist in the data
		if (sortedData[0].propsUsed.includes(1)) {
			datasets.push({
				label: 'Average Price',
				data: sortedData.map((item) => parseFloat(item.valueAvg || '0')),
				borderColor: '#4c6ef5',
				backgroundColor: 'rgba(76, 110, 245, 0.1)',
				tension: 0.3,
				fill: false
			});
		}

		if (sortedData[0].propsUsed.includes(2)) {
			datasets.push({
				label: 'Minimum Price',
				data: sortedData.map((item) => parseFloat(item.valueMin || '0')),
				borderColor: '#37b24d',
				backgroundColor: 'rgba(55, 178, 77, 0.1)',
				tension: 0.3,
				fill: false
				//borderDash: [5, 5]
			});
		}

		if (sortedData[0].propsUsed.includes(3)) {
			datasets.push({
				label: 'Maximum Price',
				data: sortedData.map((item) => parseFloat(item.valueMax || '0')),
				borderColor: '#f03e3e',
				backgroundColor: 'rgba(240, 62, 62, 0.1)',
				tension: 0.3,
				fill: false,
				borderDash: [5, 5]
			});
		}

		if (sortedData[0].propsUsed.includes(4)) {
			datasets.push({
				label: 'Weekly Forecast',
				data: sortedData.map((item) => parseFloat(item.predWeekly || '0')),
				borderColor: '#fab005',
				backgroundColor: 'rgba(250, 176, 5, 0.1)',
				tension: 0.3,
				fill: false,
				borderDash: [3, 3]
			});
		}

		// Create the chart
		chart = new Chart(canvas, {
			type: 'line',
			data: { labels, datasets },
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: {
					title: {
						display: true,
						text: materialInfo
							? `Price History: ${materialInfo.materialName} (${materialInfo.unit})`
							: 'Price History',
						font: { size: 16 }
					},
					tooltip: {
						mode: 'index',
						intersect: false
					},
					zoom: {
						pan: {
							enabled: true,
							mode: 'xy',
							threshold: 5
						},
						zoom: {
							wheel: {
								enabled: true,
								speed: 0.1
							},
							pinch: {
								enabled: true
							},
							mode: 'xy'
						},
						limits: {
							y: { min: 'original', max: 'original', minRange: 1 }
						}
					}
				},
				scales: {
					x: {
						title: {
							display: true,
							text: 'Date'
						},
						grid: {
							display: false
						}
					},
					y: {
						title: {
							display: true,
							text: materialInfo ? `Price (${materialInfo.unit})` : 'Price'
						},
						beginAtZero: false
					}
				},
				interaction: {
					mode: 'nearest',
					axis: 'x',
					intersect: false
				}
			}
		});
	}

	onMount(() => {
		if (showModal) {
			createChart();
		}
	});

	onDestroy(() => {
		if (chart) {
			chart.destroy();
		}
	});

	$effect(() => {
		if (showModal && priceData && !chart) {
			setTimeout(createChart, 100); // Small delay to ensure DOM is ready
		}
	});
</script>

<!-- Modal container -->
<div>
	<button class="chart-btn" onclick={open}>
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
			<line x1="18" y1="20" x2="18" y2="10"></line>
			<line x1="12" y1="20" x2="12" y2="4"></line>
			<line x1="6" y1="20" x2="6" y2="14"></line>
		</svg>
		View Chart
	</button>

	{#if showModal}
		<div
			class="modal-backdrop"
			role="presentation"
			onclick={(e) => {
				if (e.target === e.currentTarget) {
					close();
				}
			}}
		>
			<div class="modal-content">
				<div class="modal-header">
					<h2>
						{#if materialInfo}
							Price History: {materialInfo.materialName}
						{:else}
							Price History
						{/if}
					</h2>
					<button class="close-btn" onclick={close}>×</button>
				</div>
				<div class="modal-body">
					<div class="chart-container">
						<canvas bind:this={canvas}></canvas>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>

<style>
	.chart-btn {
		display: flex;
		align-items: center;
		gap: 6px;
		background-color: #f0f2f5;
		border: 1px solid #ced4da;
		border-radius: 4px;
		padding: 6px 12px;
		font-size: 14px;
		cursor: pointer;
		transition: all 0.2s;
	}

	.chart-btn:hover {
		background-color: #e9ecef;
	}

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
		z-index: 1000;
	}

	.modal-content {
		background-color: white;
		border-radius: 8px;
		width: 90%;
		max-width: 900px;
		max-height: 90vh;
		display: flex;
		flex-direction: column;
		box-shadow: 0 5px 15px rgba(0, 0, 0, 0.3);
	}

	.modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 1rem;
		border-bottom: 1px solid #e9ecef;
	}

	.modal-header h2 {
		margin: 0;
		font-size: 1.5rem;
		color: #333;
	}

	.close-btn {
		background: none;
		border: none;
		font-size: 1.5rem;
		cursor: pointer;
		padding: 0 0.5rem;
		color: #6c757d;
	}

	.close-btn:hover {
		color: #212529;
	}

	.modal-body {
		flex: 1;
		overflow: auto;
		padding: 1rem;
	}

	.chart-container {
		position: relative;
		height: 60vh;
		width: 100%;
	}
</style>
