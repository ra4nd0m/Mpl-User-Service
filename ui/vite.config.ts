import { svelteTesting } from "@testing-library/svelte/vite";
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import * as fs from 'fs';

export default defineConfig({
    server:{
        https:{
            key: fs.readFileSync('./cert/cert.key'),
            cert: fs.readFileSync('./cert/cert.crt')
        },
        host:'127.0.0.1',
        port: 5173,
    },
    plugins: [sveltekit()],

    test: {
        workspace: [{
            extends: "./vite.config.ts",
            plugins: [svelteTesting()],

            test: {
                name: "client",
                environment: "jsdom",
                clearMocks: true,
                include: ['src/**/*.svelte.{test,spec}.{js,ts}'],
                exclude: ['src/lib/server/**'],
                setupFiles: ['./vitest-setup-client.ts']
            }
        }, {
            extends: "./vite.config.ts",

            test: {
                name: "server",
                environment: "node",
                include: ['src/**/*.{test,spec}.{js,ts}'],
                exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
            }
        }]
    }
});
