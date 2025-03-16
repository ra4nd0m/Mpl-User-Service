import { fetchWithAuth } from "$lib/api/authClient";
import { delay, ENABLE_MOCKS, favoriteMaterials } from "$lib/mock";
import { writable } from "svelte/store";

export interface FavoritesState {
    ids: number[];
    loading: boolean;
    error: string | null;
}

const initialState: FavoritesState = {
    ids: [],
    loading: false,
    error: null
};

function createFavoritesStore() {
    const { subscribe, set, update } = writable<FavoritesState>(initialState);

    const store = {
        subscribe,
        loadFavourites: async () => {
            update(state => ({ ...state, loading: true, error: null }));
            try {
                if (ENABLE_MOCKS) {
                    await delay();

                    const favoriteIds = favoriteMaterials['123'];
                    update(state => ({ ...state, ids: favoriteIds, loading: false }));
                    return;
                }

                const response = await fetchWithAuth('/userapi/data/favorites');
                if (!response.ok) {
                    throw new Error('Failed to load favorites');
                }
                const data = await response.json();
                update(state => ({ ...state, ids: data, loading: false }));
            } catch (err) {
                console.error('Error loading favorites:', err);
                update(state => ({ ...state, loading: false, error: 'Failed to load favorites' }));
            }
        },
        toggleFavorite: async (materialId: number) => {
            try {
                update(state => {
                    const ids = [...state.ids];
                    const index = ids.indexOf(materialId);
                    if (index >= 0) {
                        ids.splice(index, 1);
                    } else {
                        ids.push(materialId);
                    }
                    return { ...state, ids };
                });
                if (!ENABLE_MOCKS) {
                    await fetchWithAuth(`/userapi/data/favorites`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ materialId })
                    });
                }
            } catch (err) {
                console.error('Error toggling favorite:', err);
                await store.loadFavourites();
            }
        },
        reset: () => set(initialState)
    };
    return store;
}

export const favoritesStore = createFavoritesStore();