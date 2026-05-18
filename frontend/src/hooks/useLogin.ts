import { useNavigate } from 'react-router-dom';
import { AuthRole, useAuthContext } from '../context/AuthContext';

interface LoginFormPayload {
  email: string;
  password: string;
}

interface LoginResponse {
  ok: boolean;
  message: string | null;
}

function mapRoleToPath(role: AuthRole): string {
  if (role === 'Patient') {
    return '/dashboard';
  }

  if (role === 'Staff') {
    return '/staff/queue';
  }

  return '/admin/users';
}

export function useLogin() {
  const navigate = useNavigate();
  const { login, cookiesRequired, clearCookiesRequired } = useAuthContext();

  const loginWithRedirect = async (payload: LoginFormPayload): Promise<LoginResponse> => {
    clearCookiesRequired();

    const result = await login(payload);

    if (!result.ok) {
      return {
        ok: false,
        message: result.message,
      };
    }

    navigate(mapRoleToPath(result.role), { replace: true });

    return {
      ok: true,
      message: null,
    };
  };

  return {
    loginWithRedirect,
    cookiesRequired,
  };
}
