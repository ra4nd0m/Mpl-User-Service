# Mpl UI

A SvelteKit 2 single-page application that serves as the front-end for the **Mpl-User-Service** mono-repo. It provides authentication, a material-price workdesk, an admin panel, and a reports section — all communicating with the auth and data API back-ends over HTTPS.

---

## Table of Contents

- [Mpl UI](#mpl-ui)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Tech Stack](#tech-stack)
  - [Project Structure](#project-structure)
  - [Configuration](#configuration)
  - [Routes](#routes)
    - [Login](#login)
    - [Dashboard](#dashboard)
    - [Workdesk](#workdesk)
    - [Materials](#materials)
    - [Reports](#reports)
    - [Admin Panel](#admin-panel)
  - [API Clients](#api-clients)
  - [State Management](#state-management)
  - [Internationalisation](#internationalisation)
  - [Running the App](#running-the-app)
    - [Prerequisites](#prerequisites)
    - [Steps](#steps)
    - [Type-checking](#type-checking)
    - [Linting and formatting](#linting-and-formatting)
  - [Building for Production](#building-for-production)

---

## Overview

Mpl UI provides:

- **JWT-based authentication** — login form, token storage in the auth store, automatic token refresh on `401 Token-Expired` responses, and logout
- **Workdesk** — price-tracking tables for favourite materials with configurable date ranges, averaging modes (weekly / monthly / quarterly / yearly), drag-and-drop reordering, and per-row export
- **All Materials** — browsable table of all materials with group filtering and the ability to mark favourites
- **Reports** — file upload / download management with per-file subscription-gated access control
- **Admin Panel** — organization and user management (create, update, register), plus per-role data-exclusion filter configuration
- **Role-based UI** — admin-only sections hidden from regular users; subscription type and expiry surfaced in the navigation
- **i18n** — English and Russian translations via Paraglide JS

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | SvelteKit 2 + Svelte 5 |
| Build tool | Vite 6 |
| Language | TypeScript 5 |
| Adapter | `@sveltejs/adapter-static` (SPA mode with `fallback: 'index.html'`) |
| Charts | Chart.js 4 + chartjs-plugin-zoom |
| Drag-and-drop | svelte-dnd-action |
| i18n | Paraglide JS (`@inlang/paraglide-js`) |
| Testing | Vitest 3 + `@testing-library/svelte` + jsdom |
| Linting / formatting | ESLint 9 + Prettier 3 |

---

## Project Structure

```
ui/
├── src/
│   ├── app.html                   # HTML shell
│   ├── app.css                    # Global styles
│   ├── app.d.ts                   # Ambient type declarations
│   ├── lib/
│   │   ├── config.ts              # Reads VITE_* env vars; exposes apiBaseUrl / apiAuthUrl
│   │   ├── i18n.ts                # Locale store + switchLocale helper
│   │   ├── index.ts               # Re-exports
│   │   ├── api/
│   │   │   ├── authClient.ts      # fetchWithAuth wrapper with token injection + auto-refresh
│   │   │   ├── userClient.ts      # Favourites, materials, sources, units, metrics, settings
│   │   │   ├── adminClient.ts     # Organizations and user management (auth service)
│   │   │   └── fileClient.ts      # Report upload / download / storage usage
│   │   ├── stores/
│   │   │   ├── authStore.ts       # JWT parsing, user state, login / logout
│   │   │   ├── favouritesStore.ts # Favourite material IDs, lazy-loaded
│   │   │   └── widgetSettingStore.ts # Per-material date range and expand state
│   │   ├── paraglide/             # Generated i18n messages (do not edit manually)
│   │   └── utils/
│   │       └── dateUtil.ts        # Date formatting helpers
│   ├── routes/
│   │   ├── +layout.svelte         # Root layout (auth guard, navigation)
│   │   ├── +page.svelte           # Redirects to /dashboard/workdesk
│   │   ├── login/
│   │   │   └── +page.svelte       # Login form
│   │   └── dashboard/
│   │       ├── +layout.svelte     # Dashboard shell with sidebar navigation
│   │       ├── +page.svelte       # Redirects to /dashboard/workdesk
│   │       ├── components/
│   │       │   └── SubscriptionInfo.svelte
│   │       ├── workdesk/
│   │       │   ├── +layout.svelte
│   │       │   ├── +page.svelte   # Redirects to /dashboard/workdesk/pricetracking
│   │       │   ├── overview/
│   │       │   │   └── +page.svelte
│   │       │   └── pricetracking/
│   │       │       └── +page.svelte  # Price-tracking tables for favourite materials
│   │       ├── materials/
│   │       │   ├── +page.svelte      # All-materials table with group filter
│   │       │   └── MaterialsTable.svelte
│   │       ├── reports/
│   │       │   ├── +page.svelte      # File upload / download
│   │       │   └── AddFileModal.svelte
│   │       └── admin/
│   │           ├── +page.svelte      # Organization and user management
│   │           ├── OrgRegistrationModal.svelte
│   │           ├── UserRegistrationModal.svelte
│   │           └── filters/
│   │               └── +page.svelte  # Per-role data-exclusion filter editor
│   └── shared/
│       └── components/
│           ├── ConfirmationDialog/   # Generic confirmation modal
│           ├── GroupSelector/        # Material-group picker
│           ├── ModalBase/            # Base modal wrapper
│           └── PriceDisplay/         # Formatted price cell
├── messages/
│   ├── en.json                    # English translations
│   └── ru.json                    # Russian translations
├── static/                        # Static assets served as-is
├── cert/                          # TLS certificate for local HTTPS dev server
├── vite.config.ts                 # Vite + Vitest configuration
├── svelte.config.js               # SvelteKit adapter + alias configuration
├── tsconfig.json
├── .env                           # Default environment variables
├── .env.dev                       # Development overrides
└── .env.prod                      # Production overrides
```

---

## Configuration

Environment variables are read at build time (Vite). Override them with `.env.dev` (development) or `.env.prod` (production).

| Variable | Description | Default (`.env`) |
|---|---|---|
| `VITE_API_BASE_URL` | Base URL for the data API (`MplDbApi` / `MplUserApi`) | `https://localhost:8080/userapi` |
| `VITE_API_AUTH_URL` | Base URL for the auth API (`MplAuthService`) | `https://localhost:8080/authapi` |
| `PUBLIC_DEV_API_BASE_URL` | Data API base URL used in `DEV` mode when `VITE_API_BASE_URL` is not set | `http://localhost:5204` |
| `PUBLIC_DEV_API_AUTH_URL` | Auth API base URL used in `DEV` mode when `VITE_API_AUTH_URL` is not set | `http://localhost:5203` |
| `VITE_USE_MOCKS` | Set to `true` to enable mock API responses | `false` |
| `REAL_URL` | Public hostname for the deployed app | `https://root.urm-company.com` |

Both API URL variables are required in production. The app throws at startup if either is missing.

The dev server binds to `https://127.0.0.1:5173` and requires a TLS certificate at `cert/cert.key` / `cert/cert.crt`.

---

## Routes

### Login

**`/login`**

Full-page login form. On success, stores the JWT in `authStore`, then redirects to `/dashboard/workdesk`. The root layout redirects unauthenticated visitors here automatically.

---

### Dashboard

**`/dashboard`** (redirects to `/dashboard/workdesk`)

Shared dashboard layout (`+layout.svelte`) renders the sidebar navigation with links to Workdesk, All Materials, Reports, and (for admins) Admin Panel. The navigation also displays the current user's subscription type and expiry.

---

### Workdesk

**`/dashboard/workdesk/pricetracking`**

Price-tracking tables for each material in the user's favourites list. Features:

- Configurable date range (presets: last 7 / 30 / 90 days, or custom from / to)
- Averaging modes: None, Weekly, Monthly, Quarterly, Yearly
- Price columns: Average, Latest Average, Min, Max, Weekly Forecast, Monthly Forecast
- Drag-and-drop reordering of tables (persisted to the server via `widgetSettingStore`)
- Per-table expand/collapse state
- CSV export per material

**`/dashboard/workdesk/overview`**

Overview page (currently under development).

---

### Materials

**`/dashboard/materials`**

Paginated / filterable table of all available materials. Supports:

- Filtering by material group (via `GroupSelector`)
- Marking materials as favourites (synced with `favouritesStore`)

---

### Reports

**`/dashboard/reports`**

File management page. Subscription-gated: each file has a `requiredSubscription` level. Features:

- Upload files with a group and subscription requirement (`AddFileModal`)
- Download purchased/accessible reports
- Status tracking per file (pending / uploading / downloading / complete / error / cancelled)
- Abort in-flight uploads or downloads
- Storage usage display

---

### Admin Panel

**`/dashboard/admin`** _(Admin role required)_

Organization and user management:

- List, create, and update organizations (INN, name, subscription type, start/end dates)
- Register new users and link them to an organization or individual subscription
- Update existing users

**`/dashboard/admin/filters`** _(Admin role required)_

Per-role data-exclusion filter editor. Allows an administrator to configure which material IDs are hidden for each subscription role.

---

## API Clients

All HTTP calls go through `src/lib/api/`. Every client (except direct auth calls) uses `fetchWithAuth` from `authClient.ts`, which:

1. Injects `Authorization: Bearer <token>` from the current auth store state
2. If the response is `401` with a `Token-Expired: true` header, silently refreshes the token via `POST /refresh` and retries the original request
3. Queues requests that arrive before the first token is available

| Client | File | Responsibilities |
|---|---|---|
| `authClient` | `api/authClient.ts` | `fetchWithAuth` wrapper, token refresh, login, logout |
| `userClient` | `api/userClient.ts` | Favourites, materials, material groups, sources, units, material properties, price metrics, widget settings |
| `adminClient` | `api/adminClient.ts` | Organizations (CRUD), user registration + update (via auth service) |
| `fileClient` | `api/fileClient.ts` | Report upload, download, list, delete, storage usage |

---

## State Management

Svelte stores handle all shared client state:

| Store | File | Contents |
|---|---|---|
| `authStore` | `stores/authStore.ts` | `isAuthenticated`, parsed JWT user (`id`, `email`, `subscriptionType`, `subscriptionEnd`, `canExportData`), `roles`, `token`; exposes `setToken`, `login`, `logout` |
| `favouritesStore` | `stores/favouritesStore.ts` | Favourite material IDs; lazy-loads on first access; exposes `loadFavourites`, `addFavourite`, `removeFavourite`, `setFavourites` |
| `widgetSettingStore` | `stores/widgetSettingStore.ts` | Per-material date range and expand/collapse state for price-tracking widgets; persisted to the server on change |

---

## Internationalisation

Translations are managed by **Paraglide JS** (`@inlang/paraglide-js`). Source message files live in `messages/`:

- `messages/en.json` — English (default)
- `messages/ru.json` — Russian

Generated output is written to `src/lib/paraglide/` (do not edit manually). Import helpers from `$lib/i18n`:

```ts
import { m, locale, switchLocale } from '$lib/i18n';

// Switch locale at runtime (no page reload)
switchLocale('ru');
```

To add a new key, edit both `en.json` and `ru.json`, then run `npm run prepare` to regenerate.

---

## Running the App

### Prerequisites

- Node.js 20+
- TLS certificate at `cert/cert.key` and `cert/cert.crt` (required for the local HTTPS dev server)
- Running instances of `MplAuthService` (port 5203) and `MplDbApi` / `MplUserApi` (port 5204) — or updated `.env` values pointing elsewhere

### Steps

```bash
# 1. Install dependencies
npm install

# 2. Start the dev server
npm run dev
# App is available at https://127.0.0.1:5173
```

### Type-checking

```bash
npm run check
# or watch mode:
npm run check:watch
```

### Linting and formatting

```bash
# Check formatting and lint
npm run lint

# Auto-format
npm run format
```

---

## Building for Production

```bash
npm run build
# Output is written to build/

# Preview the production build locally
npm run preview
```

The app is built as a static SPA (`adapter-static` with `fallback: 'index.html'`). Deploy the contents of `build/` to any static file server or CDN. Point server-side 404s to `index.html` for client-side routing to work.

---
