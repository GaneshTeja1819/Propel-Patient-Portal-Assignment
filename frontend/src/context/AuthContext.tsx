import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import SessionTimeoutModal from '../components/session/SessionTimeoutModal';
import { useInactivityTimer } from '../hooks/useInactivityTimer';

export type AuthRole = 'Patient' | 'Staff' | 'Admin';

type LoginErrorType =
  | 'invalidCredentials'
  | 'accountLocked'
  | 'cookiesRequired'
  | 'missingRole'
  | 'unexpected';

export interface LoginResultSuccess {
  ok: true;
  role: AuthRole;
}

export interface LoginResultFailure {
  ok: false;
  errorType: LoginErrorType;
  message: string;
}

export type LoginResult = LoginResultSuccess | LoginResultFailure;

interface LoginPayload {
  email: string;
  password: string;
}

interface AuthContextValue {
  role: AuthRole | null;
  isAuthenticated: boolean;
  cookiesRequired: boolean;
  login: (payload: LoginPayload) => Promise<LoginResult>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<boolean>;
  clearCookiesRequired: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

function normalizeRole(role: unknown): AuthRole | null {
  if (typeof role !== 'string') {
    return null;
  }

  const trimmed = role.trim().toLowerCase();
  if (trimmed === 'patient') {
    return 'Patient';
  }

  if (trimmed === 'staff') {
    return 'Staff';
  }

  if (trimmed === 'admin') {
    return 'Admin';
  }

  return null;
}

async function postRefreshSession(): Promise<boolean> {
  try {
    const response = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      credentials: 'include',
    });

    return response.ok;
  } catch {
    return false;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const location = useLocation();
  const [role, setRole] = useState<AuthRole | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [cookiesRequired, setCookiesRequired] = useState(false);
  const [refreshFailed, setRefreshFailed] = useState(false);

  const managesSessionTimeout =
    location.pathname.startsWith('/dashboard') ||
    location.pathname.startsWith('/staff') ||
    location.pathname.startsWith('/admin');

  const timeoutEnabled = isAuthenticated && managesSessionTimeout;

  const { phase, secondsRemaining, resetTimer } = useInactivityTimer({
    timeoutMs: 15 * 60 * 1000,
    warningMs: 13 * 60 * 1000,
    enabled: timeoutEnabled,
  });

  useEffect(() => {
    if (!timeoutEnabled) {
      setRefreshFailed(false);
    }
  }, [timeoutEnabled]);

  const clearAuthState = useCallback(() => {
    setRole(null);
    setIsAuthenticated(false);
    setCookiesRequired(false);
    setRefreshFailed(false);
  }, []);

  const refreshSession = useCallback(async (): Promise<boolean> => {
    const ok = await postRefreshSession();

    return ok;
  }, []);

  const login = useCallback(async (payload: LoginPayload): Promise<LoginResult> => {
    setCookiesRequired(false);
    setRefreshFailed(false);

    try {
      const response = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({
          email: normalizeEmail(payload.email),
          password: payload.password,
        }),
      });

      if (response.status === 401) {
        setIsAuthenticated(false);
        setRole(null);
        return {
          ok: false,
          errorType: 'invalidCredentials',
          message: 'Invalid email or password',
        };
      }

      if (response.status === 423) {
        setIsAuthenticated(false);
        setRole(null);
        return {
          ok: false,
          errorType: 'accountLocked',
          message: 'Account temporarily locked',
        };
      }

      if (!response.ok) {
        setIsAuthenticated(false);
        setRole(null);
        return {
          ok: false,
          errorType: 'unexpected',
          message: 'Unable to sign in. Please try again.',
        };
      }

      const body = (await response.json()) as { role?: unknown };
      const resolvedRole = normalizeRole(body.role);

      if (!resolvedRole) {
        setIsAuthenticated(false);
        setRole(null);
        return {
          ok: false,
          errorType: 'missingRole',
          message: 'Unable to determine account role. Please contact support.',
        };
      }

      const cookiePresent = await postRefreshSession();
      if (!cookiePresent) {
        setIsAuthenticated(false);
        setRole(null);
        setCookiesRequired(true);
        return {
          ok: false,
          errorType: 'cookiesRequired',
          message: 'Cookies required',
        };
      }

      setRole(resolvedRole);
      setIsAuthenticated(true);

      return {
        ok: true,
        role: resolvedRole,
      };
    } catch {
      setIsAuthenticated(false);
      setRole(null);
      return {
        ok: false,
        errorType: 'unexpected',
        message: 'Unable to sign in. Please try again.',
      };
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await fetch('/api/v1/auth/logout', {
        method: 'POST',
        credentials: 'include',
      });
    } catch {
      // Local auth state should still be cleared when network logout fails.
    }

    clearAuthState();
  }, [clearAuthState]);

  useEffect(() => {
    if (phase !== 'expired' || !timeoutEnabled) {
      return;
    }

    void fetch('/api/v1/auth/logout', {
      method: 'POST',
      credentials: 'include',
    }).catch(() => {
      // Expired-session redirect should proceed even if network logout fails.
    });

    const redirectTimer = window.setTimeout(() => {
      clearAuthState();
      navigate('/login?reason=expired', { replace: true });
    }, 1000);

    return () => {
      window.clearTimeout(redirectTimer);
    };
  }, [clearAuthState, navigate, phase, timeoutEnabled]);

  const handleStayLoggedIn = useCallback(async () => {
    const refreshed = await refreshSession();

    if (!refreshed) {
      setRefreshFailed(true);
      return;
    }

    setRefreshFailed(false);
    resetTimer();
  }, [refreshSession, resetTimer]);

  const clearCookiesRequired = useCallback(() => {
    setCookiesRequired(false);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      role,
      isAuthenticated,
      cookiesRequired,
      login,
      logout,
      refreshSession,
      clearCookiesRequired,
    }),
    [clearCookiesRequired, cookiesRequired, isAuthenticated, login, logout, refreshSession, role]
  );

  return (
    <AuthContext.Provider value={value}>
      {children}
      {managesSessionTimeout && (phase === 'warning' || phase === 'expired') && (
        <SessionTimeoutModal
          secondsRemaining={secondsRemaining}
          onStayLoggedIn={handleStayLoggedIn}
          refreshFailed={refreshFailed}
          expired={phase === 'expired'}
        />
      )}
    </AuthContext.Provider>
  );
}

export function useAuthContext(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuthContext must be used within an AuthProvider');
  }

  return context;
}
