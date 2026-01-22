import { ReactNode } from 'react';
import { Header } from './Header';
import { Sidebar } from './Sidebar';

interface Props {
  children: ReactNode;
}

export function Layout({ children }: Props) {
  return (
    <div className="min-h-screen bg-gray-50">
      <Header />
      <div className="flex">
        <Sidebar />
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
