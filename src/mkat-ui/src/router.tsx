import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  redirect,
} from '@tanstack/react-router';
import { Layout } from './components/layout/Layout';
import { Dashboard } from './pages/Dashboard';
import { Services } from './pages/Services';
import { ServiceDetail } from './pages/ServiceDetail';
import { ServiceCreate } from './pages/ServiceCreate';
import { ServiceEdit } from './pages/ServiceEdit';
import { Alerts } from './pages/Alerts';
import { Peers } from './pages/Peers';
import { Login } from './pages/Login';

const rootRoute = createRootRoute({
  component: () => <Outlet />,
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
      <Outlet />
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
  ]),
]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
