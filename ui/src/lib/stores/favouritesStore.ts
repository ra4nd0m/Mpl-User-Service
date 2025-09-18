import { addFavorite, getFavorites, removeFavorite } from "$lib/api/userClient";
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
    let hasLoaded = false;

    const { subscribe, set, update } = writable<FavoritesState>(initialState);

    const store = {
        subscribe,
        loadFavourites: async () => {
            if (hasLoaded) return;
            update(state => ({ ...state, loading: true, error: null }));
            try {
                const data = await getFavorites();
                if (data) {
                    update(state => ({ ...state, ids: data, loading: false }));
                    hasLoaded = true;
                } else {
                    throw new Error();
                }
            } catch {
                console.error('Error loading favorites:');
                update(state => ({ ...state, loading: false, error: 'Failed to load favorites' }));
            }
        },

        addToFavorites: async (materialId: number) => {
            try {
                update(state => {
                    const ids = [...state.ids];
                    if (!ids.includes(materialId)) {
                        ids.push(materialId);
                    }
                    return { ...state, ids };
                });

                const updatedFavorites = await addFavorite(materialId);
                if (updatedFavorites) {
                    update(state => ({ ...state, ids: updatedFavorites }));
                }

            } catch (err) {
                console.error('Error adding to favorites:', err);
                await store.loadFavourites();
            }
        },

        removeFromFavorites: async (materialId: number) => {
            try {
                update(state => {
                    const ids = [...state.ids];
                    const index = ids.indexOf(materialId);
                    if (index >= 0) {
                        ids.splice(index, 1);
                    }
                    return { ...state, ids };
                });

                const updatedFavorites = await removeFavorite(materialId);
                if (updatedFavorites) {
                    update(state => ({ ...state, ids: updatedFavorites }));
                }

            } catch (err) {
                console.error('Error removing from favorites:', err);
                await store.loadFavourites();
            }
        },

        toggleFavorite: async (materialId: number) => {
            let currentState: FavoritesState | undefined;

            const unsubscribe = store.subscribe(state => {
                currentState = state;
            });
            unsubscribe();

            const isFavorited = currentState!.ids.includes(materialId);

            if (isFavorited) {
                await store.removeFromFavorites(materialId);
            } else {
                await store.addToFavorites(materialId);
            }
        },


        reset: () => set(initialState)
    };
    return store;
}

export const favoritesStore = createFavoritesStore();