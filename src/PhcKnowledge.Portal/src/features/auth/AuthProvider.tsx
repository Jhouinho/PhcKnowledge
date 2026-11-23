import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  type ReactNode
} from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { userMessageForApiError } from "../../lib/api/apiError";
import { resetAntiforgeryCache } from "../../lib/api/httpClient";
import {
  fetchSession,
  login as loginRequest,
  logout as logoutRequest,
  type PortalSession
} from "./sessionApi";

const SESSION_QUERY_KEY = ["portal", "session"] as const;

type AuthContextValue = {
  session: PortalSession | undefined;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  loginError: string | null;
  clearLoginError: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const anonymousSession: PortalSession = {
  authenticated: false,
  userId: null,
  email: null,
  displayName: null,
  roles: []
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const sessionQuery = useQuery({
    queryKey: SESSION_QUERY_KEY,
    queryFn: ({ signal }) => fetchSession(signal),
    retry: false
  });

  const loginMutation = useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      loginRequest(email, password),
    onSuccess: (session) => {
      resetAntiforgeryCache();
      queryClient.setQueryData(SESSION_QUERY_KEY, session);
    }
  });

  const logoutMutation = useMutation({
    mutationFn: logoutRequest,
    onSuccess: () => {
      resetAntiforgeryCache();
      queryClient.setQueryData(SESSION_QUERY_KEY, anonymousSession);
      void queryClient.invalidateQueries({ queryKey: ["portal"] });
    }
  });

  const login = useCallback(
    async (email: string, password: string) => {
      await loginMutation.mutateAsync({ email, password });
    },
    [loginMutation]
  );

  const logout = useCallback(async () => {
    await logoutMutation.mutateAsync();
  }, [logoutMutation]);

  const clearLoginError = useCallback(() => {
    loginMutation.reset();
  }, [loginMutation]);

  const value = useMemo<AuthContextValue>(
    () => ({
      session: sessionQuery.data,
      isLoading: sessionQuery.isLoading,
      isAuthenticated: sessionQuery.data?.authenticated === true,
      login,
      logout,
      loginError: loginMutation.error ? userMessageForApiError(loginMutation.error) : null,
      clearLoginError
    }),
    [
      sessionQuery.data,
      sessionQuery.isLoading,
      login,
      logout,
      loginMutation.error,
      clearLoginError
    ]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth deve ser usado dentro de AuthProvider.");
  }
  return context;
}
