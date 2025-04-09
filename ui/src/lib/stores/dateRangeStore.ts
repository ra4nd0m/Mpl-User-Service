import { writable } from "svelte/store";

const today = new Date();
const defaultStartDate = new Date(today.getFullYear(), today.getMonth() - 1).toISOString().split('T')[0];
const defaultEndDate = today.toISOString().split('T')[0];

const storedStartDate = typeof localStorage !== 'undefined' ? localStorage.getItem('startDate') : null;
const storedEndDate = typeof localStorage !== 'undefined' ? localStorage.getItem('endDate') : null;

interface DateRangeState {
    startDate: string;
    endDate: string;
}

const initialState: DateRangeState = {
    startDate: storedStartDate || defaultStartDate,
    endDate: storedEndDate || defaultEndDate
};

const createDateRangeStore = () => {
    const { subscribe, set, update } = writable<DateRangeState>(initialState);

    return {
        subscribe,
        setDateRange: (startDate: string, endDate: string) => {
            if (typeof localStorage !== 'undefined') {
                localStorage.setItem('startDate', startDate);
                localStorage.setItem('endDate', endDate);
            }
            update(() => ({ startDate, endDate }));
        },
        reset: () => {
            if (typeof localStorage !== 'undefined') {
                localStorage.removeItem('startDate');
                localStorage.removeItem('endDate');
            }
            set(initialState);
        }
    }
}

export const dateRangeStore = createDateRangeStore();