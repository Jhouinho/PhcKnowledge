import { apiRequest } from "../../lib/api/httpClient";

export type PortalSession = {
  authenticated: boolean;
  userId: string | null;
  email: string | null;
  displayName: string | null;
  roles: string[];
};

export async function fetchSession(signal?: AbortSignal): Promise<PortalSession> {
  return apiRequest<PortalSession>("/api/portal/session", { signal });
}

export async function login(email: string, password: string): Promise<PortalSession> {
  await apiRequest<void>("/api/portal/auth/login", {
    method: "POST",
    requireAntiforgery: true,
    body: { email, password }
  });
  return fetchSession();
}

export async function logout(): Promise<void> {
  await apiRequest<void>("/api/portal/auth/logout", {
    method: "POST",
    requireAntiforgery: true
  });
}
