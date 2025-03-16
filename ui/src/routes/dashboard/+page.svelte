<script lang="ts">
    import { onMount } from 'svelte';
    import { favoritesStore } from '$lib/stores/favouritesStore';
    import { materials } from '$lib/mock';

    // Type definitions for the data structure
    interface MaterialInfo {
        id: number;
        name: string;
        deliveryTypeName: string;
        targetMarket: string;
        unitName: string;
    }

    interface MaterialValue {
        id: number;
        date: string;
        propsUsed: number[];
        valueAvg: string;
        valueMin: string;
        valueMax: string;
        predWeekly: string;
        predMonthly: string;
        supply: string;
        monthlyAvg: string;
        materialInfo: MaterialInfo;
    }

    interface DateEntry {
        date: string;
        materialValues: MaterialValue[];
    }
    
    // Define interface for material structure
    interface Material {
        Id: number;
        MaterialName: string;
        Source: string;
        DeliveryType: string;
        Group: string;
        Market: string;
        Unit: string;
        LastCreatedDate: string | null;
    }

    const favoriteIds = [1,2,3,4];
    let favoriteMaterials = $state<Material[]>([]); 
    let materialData = $state<DateEntry[]>([]);
    let isLoading = $state(true);

    // Sample data (in a real app, this would come from an API)
    const sampleData: DateEntry[] = [
        {
            date: "2025-03-16T00:00:00",
            materialValues: [
                {
                    id: 1,
                    date: "2025-03-16T00:00:00",
                    propsUsed: [1, 2, 3],
                    valueAvg: "10.5",
                    valueMin: "5.2",
                    valueMax: "15.8",
                    predWeekly: "11.2",
                    predMonthly: "45.6",
                    supply: "100",
                    monthlyAvg: "",
                    materialInfo: {
                        id: 1,
                        name: "Steel",
                        deliveryTypeName: "Standard",
                        targetMarket: "Construction",
                        unitName: "tons"
                    }
                },
                {
                    id: 2,
                    date: "2025-03-16T00:00:00",
                    propsUsed: [1, 4, 5],
                    valueAvg: "22.7",
                    valueMin: "8.9",
                    valueMax: "35.6",
                    predWeekly: "",
                    predMonthly: "90.1",
                    supply: "50",
                    monthlyAvg: "",
                    materialInfo: {
                        id: 2,
                        name: "Aluminum",
                        deliveryTypeName: "Express",
                        targetMarket: "Manufacturing",
                        unitName: "kg"
                    }
                }
            ]
        },
        {
            date: "2025-03-17T00:00:00",
            materialValues: [
                {
                    id: 3,
                    date: "2025-03-17T00:00:00",
                    propsUsed: [1, 2, 3],
                    valueAvg: "11.0",
                    valueMin: "5.5",
                    valueMax: "16.2",
                    predWeekly: "11.8",
                    predMonthly: "47.2",
                    supply: "95",
                    monthlyAvg: "",
                    materialInfo: {
                        id: 3,
                        name: "Steel",
                        deliveryTypeName: "Standard",
                        targetMarket: "Construction",
                        unitName: "tons"
                    }
                },
                {
                    id: 4,
                    date: "2025-03-17T00:00:00",
                    propsUsed: [1, 4, 5],
                    valueAvg: "23.1",
                    valueMin: "9.2",
                    valueMax: "36.0",
                    predWeekly: "",
                    predMonthly: "92.4",
                    supply: "48",
                    monthlyAvg: "",
                    materialInfo: {
                        id: 4,
                        name: "Aluminum",
                        deliveryTypeName: "Express",
                        targetMarket: "Manufacturing",
                        unitName: "kg"
                    }
                }
            ]
        }
    ];

    // Format date for display
    function formatDate(dateString: string): string {
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    onMount(async () => {
        // Get favorite materials info
        favoriteMaterials = materials.filter((material) => favoriteIds.includes(material.Id));
        
        // In a real app, fetch data from API
        // const response = await fetch('/api/material-values');
        // materialData = await response.json();
        
        // For now, use sample data
        materialData = sampleData;
		console.log(favoriteMaterials);
        isLoading = false;
    });
</script>

<section class="dashboard-heading">
    <h1>Dashboard</h1>
    <p>Showing market values for your {favoriteMaterials.length} favorite materials</p>
</section>

<section>
    {#if isLoading}
        <div class="loading">Loading data...</div>
    {:else if materialData.length === 0}
        <div class="no-data">No data available</div>
    {:else}
        <div class="table-container">
            <table>
                <thead>
                    <tr>
                        <th rowspan="2">Date</th>
                        {#each favoriteMaterials as material}
                            <th colspan="3">{material.MaterialName}</th>
                        {/each}
                    </tr>
                    <tr>
                        {#each favoriteMaterials as material}
                            <th>Min</th>
                            <th>Max</th>
                            <th>Avg</th>
                        {/each}
                    </tr>
                </thead>
                <tbody>
                    {#each materialData as entry}
                        <tr>
                            <td class="date-cell">{formatDate(entry.date)}</td>
                            
                            {#each favoriteMaterials as favMaterial}
                                <!-- Find if we have data for this material on this date -->
                                {@const matchedData = entry.materialValues.find(mv => 
                                    mv.materialInfo.id === favMaterial.Id
                                )}
                                
                                {#if matchedData}
                                    <td class="value-cell">{matchedData.valueMin || 'N/A'}</td>
                                    <td class="value-cell">{matchedData.valueMax || 'N/A'}</td>
                                    <td class="value-cell">{matchedData.valueAvg || 'N/A'}</td>
                                {:else}
                                    <td class="value-cell no-data-cell">-</td>
                                    <td class="value-cell no-data-cell">-</td>
                                    <td class="value-cell no-data-cell">-</td>
                                {/if}
                            {/each}
                        </tr>
                    {/each}
                </tbody>
            </table>
        </div>
    {/if}
</section>

<style>
    .dashboard-heading {
        margin-bottom: 20px;
    }

    h1 {
        margin-bottom: 0.5rem;
    }

    p {
        color: #666;
    }

    .table-container {
        overflow-x: auto;
        margin-top: 1rem;
    }

    table {
        width: 100%;
        border-collapse: collapse;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    th, td {
        border: 1px solid #ddd;
        padding: 8px;
    }

    th {
        background-color: #f2f2f2;
        text-align: center;
    }

    th[rowspan="2"] {
        vertical-align: middle;
    }

    .date-cell {
        font-weight: 500;
        background-color: #fafafa;
    }

    .value-cell {
        text-align: right;
    }

    .value-cell.no-data-cell {
        color: #aaa;
        text-align: center;
    }

    .loading {
        display: flex;
        justify-content: center;
        padding: 2rem;
        color: #666;
    }

    .no-data {
        display: flex;
        justify-content: center;
        padding: 2rem;
        color: #666;
        background-color: #f9f9f9;
        border-radius: 4px;
    }
</style>