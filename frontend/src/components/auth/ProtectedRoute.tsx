import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

/**
 * ProtectedRoute — H-002: redirects unauthenticated users to /login.
 *
 * Preserves the intended destination in router state so the login page can
 * redirect back after a successful login (react-router-dom v6 pattern).
 *
 * Usage:
 *   <Route path="/intake/:id" element={<ProtectedRoute><IntakePage /></ProtectedRoute>} />
 */
export function ProtectedRoute({ children }: { children: JSX.Element }): JSX.Element {
  const { token } = useAuth();
  const location = useLocation();

  if (!token) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
}
