<script lang="ts">
    import { getFilters as fetchFilters, pushFilter } from '$lib/api/adminClient';
    import type { DataFilter } from '$lib/api/adminClient';
    import {
        getMaterials,
        getMaterialGroups,
        getSources,
        getUnits,
        getPropertiesForDropdown,
        type Material
    } from '$lib/api/userClient';
    import { onMount } from 'svelte';
    
    let filters = $state<DataFilter[]>([]);
    let materials = $state<Material[]>([]);
    let materialGroups = $state<{ id: number; name: string }[]>([]);
    let sources = $state<{ id: number; name: string }[]>([]);
    let units = $state<{ id: number; name: string }[]>([]);
    let properties = $state<{ id: number; name: string }[]>([]);
    let loading = $state(true);
    let error = $state<string | null>(null);
    let updating = $state(false);

    // Edit mode states
    let editingFilter = $state<DataFilter | null>(null);
    let showEditModal = $state(false);

    // Create maps for quick lookups
    const materialMap = $derived(new Map(materials.map((m) => [m.id, m])));
    const groupMap = $derived(new Map(materialGroups.map((g) => [g.id, g.name])));
    const sourceMap = $derived(new Map(sources.map((s) => [s.id, s.name])));
    const unitMap = $derived(new Map(units.map((u) => [u.id, u.name])));
    const propertyMap = $derived(new Map(properties.map((p) => [p.id, p.name])));

    async function getFilters() {
        try {
            let filters = await fetchFilters();
            if (!filters) throw new Error('Failed to load filters');
            return filters;
        } catch (err) {
            console.error('Error fetching filters:', err);
            throw err;
        }
    }

    async function loadAllData() {
        try {
            loading = true;
            error = null;

            const [
                filtersData,
                materialsData,
                materialGroupsData,
                sourcesData,
                unitsData,
                propertiesData
            ] = await Promise.all([
                getFilters(),
                getMaterials(),
                getMaterialGroups(),
                getSources(),
                getUnits(),
                getPropertiesForDropdown()
            ]);

            filters = filtersData || [];
            materials = materialsData || [];
            materialGroups = materialGroupsData || [];
            sources = sourcesData || [];
            units = unitsData || [];
            properties = propertiesData || [];
        } catch (err) {
            console.error('Error loading data:', err);
            error = err instanceof Error ? err.message : 'Failed to load data';
        } finally {
            loading = false;
        }
    }

    async function updateFilter(updatedFilter: DataFilter) {
        try {
            updating = true;
            error = null;

            await pushFilter(updatedFilter);
            
            // Reload filters to get the updated data
            await loadAllData();
            
            showEditModal = false;
            editingFilter = null;
        } catch (err) {
            console.error('Error updating filter:', err);
            error = err instanceof Error ? err.message : 'Failed to update filter';
        } finally {
            updating = false;
        }
    }

    function startEdit(filter: DataFilter) {
        // Create a deep copy of the filter for editing
        editingFilter = {
            affectedRole: filter.affectedRole,
            groups: [...filter.groups],
            sources: [...filter.sources],
            units: [...filter.units],
            materialIds: [...filter.materialIds],
            properties: [...filter.properties]
        };
        showEditModal = true;
    }

    function cancelEdit() {
        showEditModal = false;
        editingFilter = null;
    }

    function addToFilter(type: 'groups' | 'sources' | 'units' | 'materialIds' | 'properties', id: number) {
        if (editingFilter && !editingFilter[type].includes(id)) {
            editingFilter[type].push(id);
        }
    }

    function removeFromFilter(type: 'groups' | 'sources' | 'units' | 'materialIds' | 'properties', id: number) {
        if (editingFilter) {
            const index = editingFilter[type].indexOf(id);
            if (index > -1) {
                editingFilter[type].splice(index, 1);
            }
        }
    }

    function getMaterialNames(materialIds: number[]): string[] {
        return materialIds
            .map((id) => materialMap.get(id)?.materialName)
            .filter((name): name is string => name !== undefined);
    }

    function getGroupNames(groupIds: number[]): string[] {
        return groupIds
            .map((id) => groupMap.get(id))
            .filter((name): name is string => name !== undefined);
    }

    function getSourceNames(sourceIds: number[]): string[] {
        return sourceIds
            .map((id) => sourceMap.get(id))
            .filter((name): name is string => name !== undefined);
    }

    function getUnitNames(unitIds: number[]): string[] {
        return unitIds
            .map((id) => unitMap.get(id))
            .filter((name): name is string => name !== undefined);
    }

    function getPropertyNames(propertyIds: number[]): string[] {
        return propertyIds
            .map((id) => propertyMap.get(id))
            .filter((name): name is string => name !== undefined);
    }

    onMount(() => {
        loadAllData();
    });
</script>

<svelte:head>
    <title>Data Filters</title>
    <meta name="description" content="Manage data access filters for different user roles." />
</svelte:head>

<section class="filters-header">
    <h1>Data Filters</h1>
    <p>Configure data access permissions and filtering rules for different user roles.</p>
</section>

{#if loading}
    <div class="loading">
        <div class="loading-spinner"></div>
        <p>Loading filters...</p>
    </div>
{:else if error}
    <div class="error">
        <p>Error: {error}</p>
        <button onclick={loadAllData} class="retry-btn">Retry</button>
    </div>
{:else if filters.length === 0}
    <div class="no-data">
        <p>No filters configured.</p>
    </div>
{:else}
    <div class="filters-container">
        {#each filters as filter}
            <div class="filter-card">
                <div class="filter-header">
                    <h3>Filter</h3>
                    <span class="role-badge" class:admin={filter.affectedRole === 'Admin'}>
                        {filter.affectedRole}
                    </span>
                </div>

                <div class="filter-content">
                    <div class="filter-section">
                        <h4>Groups</h4>
                        <div class="filter-values">
                            {#if filter.groups.length > 0}
                                {@const groupNames = getGroupNames(filter.groups)}
                                {#each groupNames as groupName}
                                    <span class="filter-tag">{groupName}</span>
                                {/each}
                                {#if groupNames.length < filter.groups.length}
                                    <span class="warning">
                                        ({filter.groups.length - groupNames.length} groups not found)
                                    </span>
                                {/if}
                            {:else}
                                <span class="empty">No groups specified</span>
                            {/if}
                        </div>
                    </div>

                    <div class="filter-section">
                        <h4>Sources</h4>
                        <div class="filter-values">
                            {#if filter.sources.length > 0}
                                {@const sourceNames = getSourceNames(filter.sources)}
                                {#each sourceNames as sourceName}
                                    <span class="filter-tag">{sourceName}</span>
                                {/each}
                                {#if sourceNames.length < filter.sources.length}
                                    <span class="warning">
                                        ({filter.sources.length - sourceNames.length} sources not found)
                                    </span>
                                {/if}
                            {:else}
                                <span class="empty">No sources specified</span>
                            {/if}
                        </div>
                    </div>

                    <div class="filter-section">
                        <h4>Units</h4>
                        <div class="filter-values">
                            {#if filter.units.length > 0}
                                {@const unitNames = getUnitNames(filter.units)}
                                {#each unitNames as unitName}
                                    <span class="filter-tag">{unitName}</span>
                                {/each}
                                {#if unitNames.length < filter.units.length}
                                    <span class="warning">
                                        ({filter.units.length - unitNames.length} units not found)
                                    </span>
                                {/if}
                            {:else}
                                <span class="empty">No units specified</span>
                            {/if}
                        </div>
                    </div>

                    <div class="filter-section">
                        <h4>Materials</h4>
                        <div class="filter-values">
                            {#if filter.materialIds.length > 0}
                                {@const materialNames = getMaterialNames(filter.materialIds)}
                                {#each materialNames as materialName}
                                    <span class="filter-tag material-tag">{materialName}</span>
                                {/each}
                                {#if materialNames.length < filter.materialIds.length}
                                    <span class="warning">
                                        ({filter.materialIds.length - materialNames.length} materials not found)
                                    </span>
                                {/if}
                            {:else}
                                <span class="empty">No materials specified</span>
                            {/if}
                        </div>
                    </div>

                    <div class="filter-section">
                        <h4>Properties</h4>
                        <div class="filter-values">
                            {#if filter.properties.length > 0}
                                {@const propertyNames = getPropertyNames(filter.properties)}
                                {#each propertyNames as propertyName}
                                    <span class="filter-tag">{propertyName}</span>
                                {/each}
                                {#if propertyNames.length < filter.properties.length}
                                    <span class="warning">
                                        ({filter.properties.length - propertyNames.length} properties not found)
                                    </span>
                                {/if}
                            {:else}
                                <span class="empty">No properties specified</span>
                            {/if}
                        </div>
                    </div>
                </div>

                <div class="filter-actions">
                    <button class="edit-btn" onclick={() => startEdit(filter)} disabled={updating}>
                        {updating ? 'Updating...' : 'Edit'}
                    </button>
                </div>
            </div>
        {/each}
    </div>
{/if}

<!-- Edit Modal -->
{#if showEditModal && editingFilter}
    <div class="modal-overlay" onclick={cancelEdit} role="presentation">
        <div class="modal-content" onclick={(e) => e.stopPropagation()} role="presentation">
            <div class="modal-header">
                <h3>Edit Filter - {editingFilter.affectedRole}</h3>
                <button class="close-btn" onclick={cancelEdit}>&times;</button>
            </div>

            <div class="modal-body">
                <div class="edit-section">
                    <h4>Groups</h4>
                    <div class="selected-items">
                        {#each editingFilter.groups as groupId}
                            {@const groupName = groupMap.get(groupId) || `Group ${groupId}`}
                            <span class="selected-tag">
                                {groupName}
                                <button class="remove-btn" onclick={() => removeFromFilter('groups', groupId)}>×</button>
                            </span>
                        {/each}
                    </div>
                    <select onchange={(e: Event) => {
                        const target = e.target as HTMLSelectElement;
                        const id = parseInt(target.value);
                        if (id) addToFilter('groups', id);
                        target.value = '';
                    }}>
                        <option value="">Add group...</option>
                        {#each materialGroups as group}
                            {#if !editingFilter.groups.includes(group.id)}
                                <option value={group.id}>{group.name}</option>
                            {/if}
                        {/each}
                    </select>
                </div>

                <div class="edit-section">
                    <h4>Sources</h4>
                    <div class="selected-items">
                        {#each editingFilter.sources as sourceId}
                            {@const sourceName = sourceMap.get(sourceId) || `Source ${sourceId}`}
                            <span class="selected-tag">
                                {sourceName}
                                <button class="remove-btn" onclick={() => removeFromFilter('sources', sourceId)}>×</button>
                            </span>
                        {/each}
                    </div>
                    <select onchange={(e: Event) => {
                        const target = e.target as HTMLSelectElement;
                        const id = parseInt(target.value);
                        if (id) addToFilter('sources', id);
                        target.value = '';
                    }}>
                        <option value="">Add source...</option>
                        {#each sources as source}
                            {#if !editingFilter.sources.includes(source.id)}
                                <option value={source.id}>{source.name}</option>
                            {/if}
                        {/each}
                    </select>
                </div>

                <div class="edit-section">
                    <h4>Units</h4>
                    <div class="selected-items">
                        {#each editingFilter.units as unitId}
                            {@const unitName = unitMap.get(unitId) || `Unit ${unitId}`}
                            <span class="selected-tag">
                                {unitName}
                                <button class="remove-btn" onclick={() => removeFromFilter('units', unitId)}>×</button>
                            </span>
                        {/each}
                    </div>
                    <select onchange={(e:Event) => {
                        const target = e.target as HTMLSelectElement;
                        const id = parseInt(target.value);
                        if (id) addToFilter('units', id);
                        target.value = '';
                    }}>
                        <option value="">Add unit...</option>
                        {#each units as unit}
                            {#if !editingFilter.units.includes(unit.id)}
                                <option value={unit.id}>{unit.name}</option>
                            {/if}
                        {/each}
                    </select>
                </div>

                <div class="edit-section">
                    <h4>Materials</h4>
                    <div class="selected-items">
                        {#each editingFilter.materialIds as materialId}
                            {@const material = materialMap.get(materialId)}
                            {@const materialName = material?.materialName || `Material ${materialId}`}
                            <span class="selected-tag material-tag">
                                {materialName}
                                <button class="remove-btn" onclick={() => removeFromFilter('materialIds', materialId)}>×</button>
                            </span>
                        {/each}
                    </div>
                    <select onchange={(e:Event) => {
                        const target = e.target as HTMLSelectElement;
                        const id = parseInt(target.value);
                        if (id) addToFilter('materialIds', id);
                        target.value = '';
                    }}>
                        <option value="">Add material...</option>
                        {#each materials as material}
                            {#if !editingFilter.materialIds.includes(material.id)}
                                <option value={material.id}>{material.materialName}</option>
                            {/if}
                        {/each}
                    </select>
                </div>

                <div class="edit-section">
                    <h4>Properties</h4>
                    <div class="selected-items">
                        {#each editingFilter.properties as propertyId}
                            {@const propertyName = propertyMap.get(propertyId) || `Property ${propertyId}`}
                            <span class="selected-tag">
                                {propertyName}
                                <button class="remove-btn" onclick={() => removeFromFilter('properties', propertyId)}>×</button>
                            </span>
                        {/each}
                    </div>
                    <select onchange={(e:Event) => {
                        const target = e.target as HTMLSelectElement;
                        const id = parseInt(target.value);
                        if (id) addToFilter('properties', id);
                        target.value = '';
                    }}>
                        <option value="">Add property...</option>
                        {#each properties as property}
                            {#if !editingFilter.properties.includes(property.id)}
                                <option value={property.id}>{property.name}</option>
                            {/if}
                        {/each}
                    </select>
                </div>
            </div>

            <div class="modal-footer">
                <button class="cancel-btn" onclick={cancelEdit} disabled={updating}>Cancel</button>
                <button class="save-btn" onclick={() => editingFilter && updateFilter(editingFilter)} disabled={updating}>
                    {updating ? 'Saving...' : 'Save Changes'}
                </button>
            </div>
        </div>
    </div>
{/if}

<style>
    .filters-header {
        margin-bottom: 2rem;
        border-bottom: 1px solid #e9ecef;
        padding-bottom: 1rem;
    }

    .filters-header h1 {
        margin-bottom: 0.5rem;
        font-size: 1.8rem;
        color: #333;
    }

    .filters-header p {
        color: #6c757d;
        line-height: 1.5;
    }

    .loading {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 3rem;
        color: #6c757d;
    }

    .loading-spinner {
        width: 32px;
        height: 32px;
        border: 3px solid #f3f3f3;
        border-top: 3px solid #007bff;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }
        100% {
            transform: rotate(360deg);
        }
    }

    .error {
        background-color: #f8d7da;
        color: #721c24;
        padding: 1rem;
        border-radius: 4px;
        text-align: center;
    }

    .retry-btn {
        margin-top: 1rem;
        padding: 0.5rem 1rem;
        background-color: #dc3545;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
    }

    .retry-btn:hover {
        background-color: #c82333;
    }

    .no-data {
        text-align: center;
        padding: 3rem;
        color: #6c757d;
        background-color: #f8f9fa;
        border-radius: 4px;
    }

    .filters-container {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
    }

    .filter-card {
        background: white;
        border: 1px solid #dee2e6;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .filter-header {
        background-color: #f8f9fa;
        padding: 1rem;
        display: flex;
        justify-content: space-between;
        align-items: center;
        border-bottom: 1px solid #dee2e6;
    }

    .filter-header h3 {
        margin: 0;
        font-size: 1.1rem;
        color: #333;
    }

    .role-badge {
        padding: 0.25rem 0.75rem;
        border-radius: 12px;
        font-size: 0.875rem;
        font-weight: 500;
        background-color: #e9ecef;
        color: #495057;
    }

    .role-badge.admin {
        background-color: #007bff;
        color: white;
    }

    .filter-content {
        padding: 1rem;
    }

    .filter-section {
        margin-bottom: 1rem;
    }

    .filter-section:last-child {
        margin-bottom: 0;
    }

    .filter-section h4 {
        margin: 0 0 0.5rem 0;
        font-size: 0.875rem;
        color: #6c757d;
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

    .filter-values {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
    }

    .filter-tag {
        display: inline-block;
        padding: 0.25rem 0.5rem;
        background-color: #e9ecef;
        color: #495057;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .material-tag {
        background-color: #d1ecf1;
        color: #0c5460;
    }

    .empty {
        color: #6c757d;
        font-style: italic;
        font-size: 0.875rem;
    }

    .warning {
        color: #856404;
        font-size: 0.75rem;
        background-color: #fff3cd;
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
    }

    .filter-actions {
        padding: 1rem;
        background-color: #f8f9fa;
        border-top: 1px solid #dee2e6;
        display: flex;
        gap: 0.5rem;
    }

    .edit-btn {
        padding: 0.5rem 1rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
        transition: background-color 0.2s;
    }

    .edit-btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .edit-btn {
        background-color: #007bff;
        color: white;
    }

    .edit-btn:hover:not(:disabled) {
        background-color: #0056b3;
    }

    /* Modal Styles */
    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
    }

    .modal-content {
        background: white;
        border-radius: 8px;
        width: 90%;
        max-width: 600px;
        max-height: 80vh;
        overflow: hidden;
        display: flex;
        flex-direction: column;
    }

    .modal-header {
        padding: 1rem;
        border-bottom: 1px solid #dee2e6;
        display: flex;
        justify-content: space-between;
        align-items: center;
        background-color: #f8f9fa;
    }

    .modal-header h3 {
        margin: 0;
        color: #333;
    }

    .close-btn {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        color: #6c757d;
        padding: 0;
        width: 30px;
        height: 30px;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .close-btn:hover {
        color: #dc3545;
    }

    .modal-body {
        padding: 1rem;
        overflow-y: auto;
        flex: 1;
    }

    .edit-section {
        margin-bottom: 1.5rem;
    }

    .edit-section h4 {
        margin: 0 0 0.5rem 0;
        color: #495057;
        font-size: 0.9rem;
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

    .selected-items {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
        min-height: 2rem;
        padding: 0.5rem;
        border: 1px solid #dee2e6;
        border-radius: 4px;
        background-color: #f8f9fa;
    }

    .selected-tag {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        padding: 0.25rem 0.5rem;
        background-color: #e9ecef;
        color: #495057;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .selected-tag.material-tag {
        background-color: #d1ecf1;
        color: #0c5460;
    }

    .remove-btn {
        background: none;
        border: none;
        color: #dc3545;
        cursor: pointer;
        font-weight: bold;
        padding: 0;
        margin-left: 0.25rem;
    }

    .remove-btn:hover {
        color: #c82333;
    }

    .edit-section select {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.875rem;
    }

    .modal-footer {
        padding: 1rem;
        border-top: 1px solid #dee2e6;
        display: flex;
        gap: 0.5rem;
        justify-content: flex-end;
        background-color: #f8f9fa;
    }

    .cancel-btn,
    .save-btn {
        padding: 0.5rem 1rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
        transition: background-color 0.2s;
    }

    .cancel-btn {
        background-color: #6c757d;
        color: white;
    }

    .cancel-btn:hover:not(:disabled) {
        background-color: #5a6268;
    }

    .save-btn {
        background-color: #28a745;
        color: white;
    }

    .save-btn:hover:not(:disabled) {
        background-color: #218838;
    }

    .cancel-btn:disabled,
    .save-btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }
</style>