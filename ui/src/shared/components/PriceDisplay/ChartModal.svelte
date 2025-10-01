<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { browser } from '$app/environment';
	import { m, getLocale } from '$lib/i18n';

	let Chart: any;
	let zoomPlugin: any;

	const { priceData, materialInfo, filteredData, aggregatesChosen } = $props();

	let canvas: HTMLCanvasElement | null = $state(null);
	let chart: any | null = null;

	function formatDate(dateString: string): string {
		const currentLocale = getLocale();
		return new Date(dateString).toLocaleDateString(currentLocale, {
			year: 'numeric',
			month: 'short',
			day: 'numeric'
		});
	}

	async function createChart() {
		if (!browser) return;
		if (chart) {
			chart.destroy();
			chart = null;
		}

		// Use filteredData if aggregates are chosen, otherwise use priceData
		const dataToUse = aggregatesChosen && aggregatesChosen.length > 0 ? filteredData : priceData;

		if (!canvas || !dataToUse || dataToUse.length === 0) return;

		if (!Chart) {
			try {
				const chartModule = await import('chart.js/auto');
				Chart = chartModule.default;

				const zoomPluginModule = await import('chartjs-plugin-zoom');
				zoomPlugin = zoomPluginModule.default;
				Chart.register(zoomPlugin);
			} catch (error) {
				console.error('Error loading Chart.js or zoom plugin:', error);
				return;
			}
		}

		let labels: string[] = [];
		let datasets: any[] = [];

		if (aggregatesChosen && aggregatesChosen.length > 0) {
			//Invert the data, as default is newest entries first
			const invertedFilteredData = [...filteredData].reverse();
			// Handle filtered aggregate data
			labels = invertedFilteredData.map((item) => item.date);

			const aggregateType = aggregatesChosen[0];
			let labelName = '';
			let borderColor = '';

			switch (aggregateType) {
				case 'weekly':
					labelName = m.workdesk_price_tracking_table_head_price_average_weekly();
					borderColor = '#d63384';
					break;
				case 'monthly':
					labelName = m.workdesk_price_tracking_table_head_price_average_monthly();
					borderColor = '#0d6efd';
					break;
				case 'quarterly':
					labelName = m.workdesk_price_tracking_table_head_price_average_quarterly();
					borderColor = '#198754';
					break;
				case 'yearly':
					labelName = m.workdesk_price_tracking_table_head_price_average_yearly();
					borderColor = '#fd7e14';
					break;
				default:
					labelName = m.workdesk_price_tracking_table_head_price_average();
					borderColor = '#4c6ef5';
			}

			datasets.push({
				label: labelName,
				data: invertedFilteredData.map((item) => (item.value ? parseFloat(item.value) : null)),
				borderColor: borderColor,
				backgroundColor: borderColor.replace(')', ', 0.1)').replace('rgb', 'rgba'),
				tension: 0.3,
				fill: false
			});
		} else {
			// Handle regular price data - existing logic
			const sortedData = [...priceData].sort(
				(a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
			);

			labels = sortedData.map((item) => formatDate(item.date));

			// Only add datasets for props that exist in the data
			if (sortedData[0].propsUsed.includes(1)) {
				datasets.push({
					label: m.workdesk_price_tracking_table_head_price_average(),
					data: sortedData.map((item) => (item.valueAvg ? parseFloat(item.valueAvg) : null)),
					borderColor: '#4c6ef5',
					backgroundColor: 'rgba(76, 110, 245, 0.1)',
					tension: 0.3,
					fill: false
				});
			}

			if (sortedData[0].propsUsed.includes(2)) {
				datasets.push({
					label: m.workdesk_price_tracking_table_head_price_min(),
					data: sortedData.map((item) => (item.valueMin ? parseFloat(item.valueMin) : null)),
					borderColor: '#37b24d',
					backgroundColor: 'rgba(55, 178, 77, 0.1)',
					tension: 0.3,
					fill: false,
					borderDash: [5, 5]
				});
			}

			if (sortedData[0].propsUsed.includes(3)) {
				datasets.push({
					label: m.workdesk_price_tracking_table_head_price_max(),
					data: sortedData.map((item) => (item.valueMax ? parseFloat(item.valueMax) : null)),
					borderColor: '#f03e3e',
					backgroundColor: 'rgba(240, 62, 62, 0.1)',
					tension: 0.3,
					fill: false,
					borderDash: [5, 5]
				});
			}

			if (sortedData[0].propsUsed.includes(4)) {
				datasets.push({
					label: m.workdesk_price_tracking_table_head_price_forecast_weekly(),
					data: sortedData.map((item) => (item.predWeekly ? parseFloat(item.predWeekly) : null)),
					borderColor: '#fab005',
					backgroundColor: 'rgba(250, 176, 5, 0.1)',
					tension: 0.3,
					fill: false,
					borderDash: [3, 3]
				});
			}

			if (sortedData[0].propsUsed.includes(-1)) {
				datasets.push({
					label: 'Weekly Average',
					data: sortedData.map((item) => (item.weeklyAvg ? parseFloat(item.weeklyAvg) : null)),
					borderColor: '#d63384',
					backgroundColor: 'rgba(214, 51, 132, 0.1)',
					tension: 0.3,
					fill: false
				});
			}

			if (sortedData[0].propsUsed.includes(-2)) {
				datasets.push({
					label: 'Monthly Average',
					data: sortedData.map((item) => (item.monthlyAvg ? parseFloat(item.monthlyAvg) : null)),
					borderColor: '#0d6efd',
					backgroundColor: 'rgba(13, 110, 253, 0.1)',
					tension: 0.3,
					fill: false
				});
			}

			if (sortedData[0].propsUsed.includes(-3)) {
				datasets.push({
					label: 'Quarterly Average',
					data: sortedData.map((item) =>
						item.quarterlyAvg ? parseFloat(item.quarterlyAvg) : null
					),
					borderColor: '#198754',
					backgroundColor: 'rgba(25, 135, 84, 0.1)',
					tension: 0.3,
					fill: false
				});
			}

			if (sortedData[0].propsUsed.includes(-4)) {
				datasets.push({
					label: 'Yearly Average',
					data: sortedData.map((item) => (item.yearlyAvg ? parseFloat(item.yearlyAvg) : null)),
					borderColor: '#fd7e14',
					backgroundColor: 'rgba(253, 126, 20, 0.1)',
					tension: 0.3,
					fill: false
				});
			}
		}

		// Create the chart
		chart = new Chart(canvas, {
			type: 'line',
			data: { labels, datasets },
			options: {
				responsive: true,
				maintainAspectRatio: false,
				elements: {
					line: {
						spanGaps: true
					}
				},
				plugins: {
					title: {
						display: true,
						text: materialInfo
							? `${m.workdesk_price_tracking_chart_price_history()}: ${materialInfo.materialName} (${materialInfo.unit})`
							: m.workdesk_price_tracking_chart_price_history(),
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
							text:
								aggregatesChosen && aggregatesChosen.length > 0
									? m.workdesk_price_tracking_chart_period()
									: m.workdesk_price_tracking_chart_date()
						},
						grid: {
							display: false
						}
					},
					y: {
						title: {
							display: true,
							text: materialInfo
								? `${m.workdesk_price_tracking_chart_price()} (${materialInfo.unit})`
								: m.workdesk_price_tracking_chart_price()
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
		createChart();
	});

	onDestroy(() => {
		if (chart) {
			chart.destroy();
		}
	});

	$effect(() => {
		if (priceData && !chart) {
			setTimeout(createChart, 100); // Small delay to ensure DOM is ready
		}
	});
</script>

<div class="chart-container">
	<canvas bind:this={canvas}></canvas>
</div>

<style>
	.chart-container {
		position: relative;
		height: 60vh;
		width: 100%;
	}
</style>
