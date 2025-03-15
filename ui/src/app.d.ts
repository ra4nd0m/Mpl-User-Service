// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
	interface ImportMetaEnv {
		PUBLIC_API_BASE_URL: string;
		PUBLIC_API_AUTH_URL: string;
	}

	interface ImportMeta {
		readonly env: ImportMetaEnv;
	}
}


export { };
