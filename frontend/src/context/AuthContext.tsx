/**
 * AuthContext.tsx — Stub for US_010 (Patient Login).
 * Provides the authenticated user identity consumed by intake pages.
 * Replace with the full implementation when US_010 is delivered.
 */
import { createContext, useContext } from 'react';

export interface AuthUser {
  id: string;
  displayName: string;
  role: 'Patient' | 'Staff' | 'Admin';
}

export interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
}

// DEV stub: provides a mock authenticated user until US_010 login is built.
const devDefault: AuthContextValue = import.meta.env.DEV
  ? { user: { id: 'ddab4743-1cfc-42f2-9bbd-a36b7d50b6a1', displayName: 'Test Patient', role: 'Patient' }, token: 'dev-token' }
  : { user: null, token: null };

export const AuthContext = createContext<AuthContextValue>(devDefault);

export function useAuth(): AuthContextValue {
  return useContext(AuthContext);
}
