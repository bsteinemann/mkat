# Route-Based Code Splitting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split the 584KB single JS bundle into per-route chunks using React.lazy() so users only download the page they navigate to.

**Architecture:** Convert all static page imports in `router.tsx` to `React.lazy()` dynamic imports. Wrap the route outlet in a `<Suspense>` boundary with a loading fallback. Vite automatically code-splits on dynamic `import()` boundaries — no Rollup config needed.

**Tech Stack:** React 19 (lazy/Suspense), Vite 7 (automatic chunk splitting), TanStack Router

---

## Task 1: Add Suspense Boundary and Lazy-Load All Routes

This is a single, focused change — convert static imports to lazy imports and add a loading fallback.

**Files:**
- Modify: `src/mkat-ui/src/router.tsx`

**Step 1: Replace static page imports with React.lazy()**

Change `router.tsx` from:
```tsx
import { Dashboard } from './pages/Dashboard';
import { Services } from './pages/Services';
import { ServiceDetail } from './pages/ServiceDetail';
import { ServiceCreate } from './pages/ServiceCreate';
import { ServiceEdit } from './pages/ServiceEdit';
import { Alerts } from './pages/Alerts';
import { Peers } from './pages/Peers';
import { Contacts } from './pages/Contacts';
import { Login } from './pages/Login';
```

To:
```tsx
import { lazy, Suspense } from 'react';

const Dashboard = lazy(() => import('./pages/Dashboard').then(m => ({ default: m.Dashboard })));
const Services = lazy(() => import('./pages/Services').then(m => ({ default: m.Services })));
const ServiceDetail = lazy(() => import('./pages/ServiceDetail').then(m => ({ default: m.ServiceDetail })));
const ServiceCreate = lazy(() => import('./pages/ServiceCreate').then(m => ({ default: m.ServiceCreate })));
const ServiceEdit = lazy(() => import('./pages/ServiceEdit').then(m => ({ default: m.ServiceEdit })));
const Alerts = lazy(() => import('./pages/Alerts').then(m => ({ default: m.Alerts })));
const Peers = lazy(() => import('./pages/Peers').then(m => ({ default: m.Peers })));
const Contacts = lazy(() => import('./pages/Contacts').then(m => ({ default: m.Contacts })));
const Login = lazy(() => import('./pages/Login').then(m => ({ default: m.Login })));
```

Note: The `.then(m => ({ default: m.X }))` pattern is needed because the pages use named exports (`export function Dashboard`), not default exports. `React.lazy()` requires a default export.

**Step 2: Add Suspense boundary to the authenticated route**

Wrap the `<Outlet />` in both the root route and authenticated route with `<Suspense>`:

```tsx
const rootRoute = createRootRoute({
  component: () => (
    <Suspense fallback={null}>
      <Outlet />
    </Suspense>
  ),
});
```

```tsx
const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'authenticated',
  beforeLoad: () => {
    if (!localStorage.getItem('mkat_credentials')) {
      throw redirect({ to: '/login' });
    }
  },
  component: () => (
    <Layout>
      <Suspense fallback={<div className="p-6">Loading...</div>}>
        <Outlet />
      </Suspense>
    </Layout>
  ),
});
```

The root-level `Suspense` (with `fallback={null}`) covers the Login page lazy load. The authenticated-route `Suspense` covers all dashboard pages and shows "Loading..." inside the layout shell so the sidebar/header remain visible during navigation.

**Step 3: Verify build produces multiple chunks**

```bash
cd src/mkat-ui && npm run build
```

Expected: Multiple JS files in the output instead of one large `index-*.js`. Each page becomes its own chunk. The main bundle should be significantly smaller than 584KB.

**Step 4: Verify the app works**

```bash
cd src/mkat-ui && npm run dev
```

- Navigate to each page (Dashboard, Services, Service Detail, Alerts, Peers, Contacts, Login)
- Confirm pages load correctly
- Check browser DevTools Network tab to see chunks loading on navigation

**Step 5: Commit**

```bash
git add src/mkat-ui/src/router.tsx
git commit -m "perf: add route-based code splitting with React.lazy"
```

---

## Notes

- **Layout, Sidebar, Header** stay in the main bundle (they're always visible). This is correct — only page content should be lazy-loaded.
- **Shared dependencies** (React, TanStack, Radix primitives) will be in the main chunk since they're used across pages. Vite handles this automatically.
- **No loading spinner needed** — page chunks are small and load nearly instantly on any connection. A simple "Loading..." text is sufficient.
- **No preloading needed** — the app is small enough that on-demand loading is fine. If desired later, TanStack Router supports `route.preload()` on hover.
