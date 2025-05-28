import { writable, get } from 'svelte/store';

export interface DateRangeSetting {
    startDate: string;
    endDate: string;
}

export interface WidgetSettings {
    priceTable: {
        [materialId: string]: {
            dateRange?: DateRangeSetting;
            isExpanded?: boolean;
        };
    };
    // Can expand with other widget types in the future
    // otherWidget: { [id: string]: OtherWidgetSettings };
}

// Default date ranges
const getDefaultDateRange = (): DateRangeSetting => {
    const today = new Date();
    return {
        endDate: today.toISOString().split('T')[0],
        startDate: new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
    };
};

// Initialize store with data from localStorage or defaults
const initializeStore = (): WidgetSettings => {
    if (typeof localStorage === 'undefined') {
        return { priceTable: {} };
    }
    
    const storedSettings = localStorage.getItem('widgetSettings');
    if (storedSettings) {
        try {
            return JSON.parse(storedSettings);
        } catch (e) {
            console.error('Error parsing widget settings from localStorage', e);
        }
    }
    
    return { priceTable: {} };
};

// Create the store
const createWidgetSettingsStore = () => {
    const { subscribe, update, set } = writable<WidgetSettings>(initializeStore());
    
    // Persist settings to localStorage
    const persistSettings = (settings: WidgetSettings) => {
        if (typeof localStorage !== 'undefined') {
            localStorage.setItem('widgetSettings', JSON.stringify(settings));
        }
    };
    
    return {
        subscribe,
        
        // Set date range for a specific price table
        setPriceTableDateRange: (materialId: number | string, dateRange: DateRangeSetting) => {
            update(settings => {
                const id = materialId.toString();
                if (!settings.priceTable) {
                    settings.priceTable = {};
                }
                if (!settings.priceTable[id]) {
                    settings.priceTable[id] = {};
                }
                settings.priceTable[id].dateRange = dateRange;
                persistSettings(settings);
                return settings;
            });
        },
        
        // Get date range for a specific price table (with fallback to defaults)
        getPriceTableDateRange: (materialId: number | string): DateRangeSetting => {
            const id = materialId.toString();
            const settings = get({ subscribe });
            
            if (settings.priceTable && settings.priceTable[id] && settings.priceTable[id].dateRange) {
                return settings.priceTable[id].dateRange as DateRangeSetting;
            }
            
            return getDefaultDateRange();
        },
        
        // Set expanded state for a specific price table
        setPriceTableExpanded: (materialId: number | string, isExpanded: boolean) => {
            update(settings => {
                const id = materialId.toString();
                if (!settings.priceTable) {
                    settings.priceTable = {};
                }
                if (!settings.priceTable[id]) {
                    settings.priceTable[id] = {};
                }
                settings.priceTable[id].isExpanded = isExpanded;
                persistSettings(settings);
                return settings;
            });
        },
        
        // Get expanded state for a specific price table (defaults to true)
        getPriceTableExpanded: (materialId: number | string): boolean => {
            const id = materialId.toString();
            const settings = get({ subscribe });
            
            if (settings.priceTable && settings.priceTable[id] && settings.priceTable[id].isExpanded !== undefined) {
                return settings.priceTable[id].isExpanded as boolean;
            }
            
            return true; // Default to expanded
        },
        
        // Reset settings for a specific price table
        resetPriceTableSettings: (materialId: number | string) => {
            update(settings => {
                const id = materialId.toString();
                if (settings.priceTable && settings.priceTable[id]) {
                    delete settings.priceTable[id];
                    persistSettings(settings);
                }
                return settings;
            });
        },
        
        // Reset all settings
        resetAll: () => {
            const initialSettings = { priceTable: {} };
            set(initialSettings);
            persistSettings(initialSettings);
        }
    };
};

export const widgetSettingsStore = createWidgetSettingsStore();