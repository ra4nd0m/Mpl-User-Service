<script lang="ts">
    import type { Material, MaterialDateMetricsResp } from '$lib/api/userClient';
    import { onDestroy, onMount } from 'svelte';
    import { browser } from '$app/environment';

    let Chart: any;
    let zoomPlugin: any;

    const { priceData, materialInfo, filteredData, aggregatesChosen } = $props();

    let canvas: HTMLCanvasElement | null = $state(null);
    let chart: any | null = null;
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
                    labelName = 'Weekly Average';
                    borderColor = '#d63384';
                    break;
                case 'monthly':
                    labelName = 'Monthly Average';
                    borderColor = '#0d6efd';
                    break;
                case 'quarterly':
                    labelName = 'Quarterly Average';
                    borderColor = '#198754';
                    break;
                case 'yearly':
                    labelName = 'Yearly Average';
                    borderColor = '#fd7e14';
                    break;
                default:
                    labelName = 'Average';
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
                    label: 'Average Price',
                    data: sortedData.map((item) => (item.valueAvg ? parseFloat(item.valueAvg) : null)),
                    borderColor: '#4c6ef5',
                    backgroundColor: 'rgba(76, 110, 245, 0.1)',
                    tension: 0.3,
                    fill: false
                });
            }

            if (sortedData[0].propsUsed.includes(2)) {
                datasets.push({
                    label: 'Minimum Price',
                    data: sortedData.map((item) => (item.valueMin ? parseFloat(item.valueMin) : null)),
                    borderColor: '#37b24d',
                    backgroundColor: 'rgba(55, 178, 77, 0.1)',
                    tension: 0.3,
                    fill: false
                });
            }

            if (sortedData[0].propsUsed.includes(3)) {
                datasets.push({
                    label: 'Maximum Price',
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
                    label: 'Weekly Forecast',
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

            if(sortedData[0].propsUsed.includes(-2)) {
                datasets.push({
                    label: 'Monthly Average',
                    data: sortedData.map((item) => (item.monthlyAvg ? parseFloat(item.monthlyAvg) : null)),
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    tension: 0.3,
                    fill: false
                });
            }

            if(sortedData[0].propsUsed.includes(-3)) {
                datasets.push({
                    label: 'Quarterly Average',
                    data: sortedData.map((item) => (item.quarterlyAvg ? parseFloat(item.quarterlyAvg) : null)),
                    borderColor: '#198754',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    tension: 0.3,
                    fill: false
                });
            }

            if(sortedData[0].propsUsed.includes(-4)) {
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
                            text: aggregatesChosen && aggregatesChosen.length > 0 ? 'Period' : 'Date'
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

	function handleEscKey(event: KeyboardEvent) {
		console.log(event.key);
		if (event.key === 'Escape' && showModal) {
			close();
		}
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
<div onkeydown={handleEscKey} role="presentation">
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
