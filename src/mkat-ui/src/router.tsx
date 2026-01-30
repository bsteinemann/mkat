import { lazy, Suspense } from 'react';
import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  redirect,
} from '@tanstack/react-router';
import { getBasePath } from './config';
import { Layout } from './components/layout/Layout';

const Dashboard = lazy(() => import('./pages/Dashboard').then((m) => ({ default: m.Dashboard })));
const Services = lazy(() => import('./pages/Services').then((m) => ({ default: m.Services })));
const ServiceDetail = lazy(() =>
  import('./pages/ServiceDetail').then((m) => ({ default: m.ServiceDetail })),
);
const ServiceCreate = lazy(() =>
  import('./pages/ServiceCreate').then((m) => ({ default: m.ServiceCreate })),
);
const ServiceEdit = lazy(() =>
  import('./pages/ServiceEdit').then((m) => ({ default: m.ServiceEdit })),
);
const Alerts = lazy(() => import('./pages/Alerts').then((m) => ({ default: m.Alerts })));
const Peers = lazy(() => import('./pages/Peers').then((m) => ({ default: m.Peers })));
const Contacts = lazy(() => import('./pages/Contacts').then((m) => ({ default: m.Contacts })));
const DependencyMap = lazy(() =>
  import('./pages/DependencyMap').then((m) => ({ default: m.DependencyMap })),
);
const Login = lazy(() => import('./pages/Login').then((m) => ({ default: m.Login })));

const rootRoute = createRootRoute({
  component: () => (
    <Suspense fallback={null}>
      <Outlet />
    </Suspense>
  ),
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: Login,
});

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

const dashboardRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/',
  component: Dashboard,
});

const servicesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services',
  component: Services,
});

const serviceCreateRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/new',
  component: ServiceCreate,
});

const serviceDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/$serviceId',
  component: ServiceDetail,
});

const serviceEditRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/services/$serviceId/edit',
  component: ServiceEdit,
});

const alertsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/alerts',
  component: Alerts,
});

const peersRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/peers',
  component: Peers,
});

const contactsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/contacts',
  component: Contacts,
});

const dependencyMapRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/dependencies',
  component: DependencyMap,
});

const routeTree = rootRoute.addChildren([
  loginRoute,
  authenticatedRoute.addChildren([
    dashboardRoute,
    servicesRoute,
    serviceCreateRoute,
    serviceDetailRoute,
    serviceEditRoute,
    alertsRoute,
    peersRoute,
    contactsRoute,
    dependencyMapRoute,
  ]),
]);

export const router = createRouter({ routeTree, basepath: getBasePath() });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
