import { writable } from 'svelte/store';
import { setLocale, getLocale } from '$lib/paraglide/runtime';
import * as t from '$lib/paraglide/messages';

export const m = t;
export { getLocale };
export const locales = ['en', 'ru'] as const;
export const locale = writable(getLocale());
export function switchLocale(
    to: typeof locales[number]
) {
    setLocale(to, { reload: false });
    locale.set(to)
}
