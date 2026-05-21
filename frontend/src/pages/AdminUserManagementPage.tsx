import { type ReactNode, useEffect } from 'react';
import { Navigate } from 'react-router-dom';
import { AdminPortalLayout } from '../components/admin/AdminPortalLayout';
import { UserSearchPanel } from '../components/admin/UserSearchPanel';
import type { AuthRole } from '../context/AuthContext';
import { useAuthContext } from '../context/AuthContext';
import { useAdminUsers } from '../hooks/useAdminUsers';
import styles from './AdminUserManagementPage.module.css';

interface RequireRoleProps {
  role: AuthRole;
  children: ReactNode;
}

function RequireRole({ role, children }: RequireRoleProps) {
  const { isAuthenticated, role: userRole } = useAuthContext();
  if (!isAuthenticated || userRole !== role) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

function AdminUserManagementContent() {
  const { users, loading, error, searchUsers, deactivateUser, reactivateUser, changeRole } =
    useAdminUsers();

  useEffect(() => {
    void searchUsers('');
  }, [searchUsers]);

  return (
    <main className={styles.page} role="main">
      <header className={styles.pageHeader}>
        <h1 className={styles.pageTitle}>User Management</h1>
      </header>
      <UserSearchPanel
        users={users}
        loading={loading}
        error={error}
        onSearch={searchUsers}
        onDeactivate={deactivateUser}
        onReactivate={reactivateUser}
        onRoleChange={changeRole}
      />
    </main>
  );
}

function AdminUserManagementPage() {
  return (
    <RequireRole role="Admin">
      <AdminPortalLayout>
        <AdminUserManagementContent />
      </AdminPortalLayout>
    </RequireRole>
  );
}

export default AdminUserManagementPage;
