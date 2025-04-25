// filepath: /home/egor/projects/Mpl-User-Service/ui/src/routes/+page.ts
import { browser } from '$app/environment';
import { refreshAccessToken } from '$lib/api/authClient';
// Remove authStore import if not used elsewhere in this file
// import { authStore } from '$lib/stores/authStore';
import type { PageLoad } from './$types'; // Import the type

export const load: PageLoad = async () => {
    let token: string | null = null;
    if (browser) {
        console.log('Fetching token in +page.ts load...');
        try {
            // Pass fetch if needed by your API client
            token = await refreshAccessToken();
            console.log('Token fetched in load:', token ? 'Success' : 'Failed');
        } catch (error) {
            console.error('Error fetching token in load:', error);
            token = null; // Ensure token is null on error
        }
    } else {
        console.log('Skipping token fetch in +page.ts load (server)');
    }

    // Return the fetched token (or null)
    return {
        token: token
    };
};