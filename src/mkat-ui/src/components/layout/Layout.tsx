import { type ReactNode, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';
import { Toaster } from '@/components/ui/sonner';
import { initNotifications } from '../../notifications';
import { connectSSE } from '../../sse';

interface Props {
  children: ReactNode;
}

export function Layout({ children }: Props) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const credentials = localStorage.getItem('mkat_credentials');
    if (!credentials) return;

    initNotifications();

    const disconnect = connectSSE((type, data) => {
      if (type === 'alert_dispatched' && Notification.permission === 'granted') {
        const alertData = data as { message?: string };
        new Notification('mkat Alert', {
          body: alertData.message || 'New alert',
          icon: './icons/icon-192.png',
        });
      }
      queryClient.invalidateQueries({ queryKey: ['alerts'] });
      queryClient.invalidateQueries({ queryKey: ['services'] });
    });

    return () => disconnect();
  }, [queryClient]);

  return (
    <SidebarProvider>
      <Sidebar />
      <SidebarInset>
        <Header />
        <main className="flex-1 p-6">{children}</main>
      </SidebarInset>
      <Toaster />
    </SidebarProvider>
  );
}
