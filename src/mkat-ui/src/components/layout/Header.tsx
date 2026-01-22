import { useNavigate } from '@tanstack/react-router';

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
        <button
          onClick={handleLogout}
          className="text-sm text-gray-600 hover:text-gray-900"
        >
          Logout
        </button>
      </div>
    </header>
  );
}
