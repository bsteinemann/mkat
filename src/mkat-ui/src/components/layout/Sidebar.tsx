import { Link, useRouterState } from '@tanstack/react-router';
import { LayoutDashboard, Server, Bell, Link2, Users, GitBranch } from 'lucide-react';
import {
  Sidebar as SidebarRoot,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@/components/ui/sidebar';
import { ThemeToggle } from '@/components/ThemeToggle';

const navItems = [
  { to: '/' as const, label: 'Dashboard', icon: LayoutDashboard },
  { to: '/services' as const, label: 'Services', icon: Server },
  { to: '/dependencies' as const, label: 'Dependencies', icon: GitBranch },
  { to: '/alerts' as const, label: 'Alerts', icon: Bell },
  { to: '/peers' as const, label: 'Peers', icon: Link2 },
  { to: '/contacts' as const, label: 'Contacts', icon: Users },
];

export function Sidebar() {
  const routerState = useRouterState();
  const currentPath = routerState.location.pathname;

  return (
    <SidebarRoot>
      <SidebarHeader className="px-4 py-3">
        <span className="text-xl font-bold">mkat</span>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Navigation</SidebarGroupLabel>
          <SidebarMenu>
            {navItems.map((item) => {
              const isActive =
                currentPath === item.to || (item.to !== '/' && currentPath.startsWith(item.to));

              return (
                <SidebarMenuItem key={item.to}>
                  <SidebarMenuButton asChild isActive={isActive} tooltip={item.label}>
                    <Link to={item.to}>
                      <item.icon />
                      <span>{item.label}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              );
            })}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <ThemeToggle />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
      <SidebarRail />
    </SidebarRoot>
  );
}
