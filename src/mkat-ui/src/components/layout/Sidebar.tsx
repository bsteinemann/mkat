import { Link, useRouterState } from '@tanstack/react-router';

const navItems = [
  { to: '/' as const, label: 'Dashboard' },
  { to: '/services' as const, label: 'Services' },
  { to: '/alerts' as const, label: 'Alerts' },
  { to: '/peers' as const, label: 'Peers' },
];

export function Sidebar() {
  const routerState = useRouterState();
  const currentPath = routerState.location.pathname;

  return (
    <aside className="w-56 bg-white border-r border-gray-200 min-h-full">
      <nav className="p-4 space-y-1">
        {navItems.map(item => {
          const isActive = currentPath === item.to ||
            (item.to !== '/' && currentPath.startsWith(item.to));

          return (
            <Link
              key={item.to}
              to={item.to}
              className={`block px-3 py-2 rounded text-sm font-medium ${
                isActive
                  ? 'bg-blue-50 text-blue-700'
                  : 'text-gray-700 hover:bg-gray-50'
              }`}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
