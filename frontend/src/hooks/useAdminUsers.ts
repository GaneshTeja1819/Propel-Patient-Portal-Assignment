import { useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { AuthRole } from '../context/AuthContext';

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  role: AuthRole;
  status: 'Active' | 'Inactive';
  lastLogin: string | null;
  isSelf?: boolean;
}

export interface UseAdminUsersReturn {
  users: AdminUser[];
  loading: boolean;
  error: string | null;
  searchUsers: (q: string) => Promise<void>;
  deactivateUser: (id: string) => Promise<void>;
  reactivateUser: (id: string) => Promise<void>;
  changeRole: (id: string, newRole: AuthRole) => Promise<void>;
}

export function useAdminUsers(): UseAdminUsersReturn {
  const navigate = useNavigate();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleAuthError = useCallback(
    (status: number): boolean => {
      if (status === 401) {
        navigate('/login?redirect=/admin/users');
        return true;
      }
      if (status === 403) {
        navigate('/admin/users');
        return true;
      }
      return false;
    },
    [navigate],
  );

  const searchUsers = useCallback(
    async (q: string): Promise<void> => {
      setLoading(true);
      setError(null);
      try {
        const qs = q ? `?q=${encodeURIComponent(q)}` : '';
        const res = await fetch(`/api/v1/admin/users${qs}`, { credentials: 'include' });
        if (handleAuthError(res.status)) return;
        if (!res.ok) {
          setError('Failed to load users. Please try again.');
          return;
        }
        const data = (await res.json()) as AdminUser[];
        setUsers(data);
      } catch {
        setError('Failed to load users. Please try again.');
      } finally {
        setLoading(false);
      }
    },
    [handleAuthError],
  );

  const deactivateUser = useCallback(
    async (id: string): Promise<void> => {
      const res = await fetch(`/api/v1/admin/users/${encodeURIComponent(id)}/deactivate`, {
        method: 'PATCH',
        credentials: 'include',
      });
      if (handleAuthError(res.status)) return;
      if (!res.ok) throw new Error('Deactivation failed.');
      setUsers(prev => prev.map(u => (u.id === id ? { ...u, status: 'Inactive' as const } : u)));
    },
    [handleAuthError],
  );

  const reactivateUser = useCallback(
    async (id: string): Promise<void> => {
      const res = await fetch(`/api/v1/admin/users/${encodeURIComponent(id)}/reactivate`, {
        method: 'PATCH',
        credentials: 'include',
      });
      if (handleAuthError(res.status)) return;
      if (!res.ok) throw new Error('Reactivation failed.');
      setUsers(prev => prev.map(u => (u.id === id ? { ...u, status: 'Active' as const } : u)));
    },
    [handleAuthError],
  );

  const changeRole = useCallback(
    async (id: string, newRole: AuthRole): Promise<void> => {
      const res = await fetch(`/api/v1/admin/users/${encodeURIComponent(id)}/role`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ role: newRole }),
      });
      if (handleAuthError(res.status)) return;
      if (!res.ok) throw new Error('Role change failed.');
      setUsers(prev => prev.map(u => (u.id === id ? { ...u, role: newRole } : u)));
    },
    [handleAuthError],
  );

  return { users, loading, error, searchUsers, deactivateUser, reactivateUser, changeRole };
}
