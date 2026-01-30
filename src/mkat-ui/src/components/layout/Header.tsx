import { useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';

export function Header() {
  const navigate = useNavigate();

  const handleLogout = () => {
    localStorage.removeItem('mkat_credentials');
    navigate({ to: '/login' });
  };

  return (
    <header className="bg-white shadow-sm border-b border-gray-200">
      <div className="flex items-center justify-between px-6 py-3">
        <h1 className="text-xl font-bold text-gray-900">mkat</h1>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleLogout}
        >
          Logout
        </Button>
      </div>
    </header>
  );
}
