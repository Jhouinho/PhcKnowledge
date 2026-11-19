import { ApiError, mapStatusToCode } from "./apiError";

export const ANTIFORGERY_PATH = "/api/portal/auth/antiforgery";
export const ANTIFORGERY_HEADER = "X-CSRF-TOKEN";

type AntiforgeryResponse = {
  token?: string;
};

type RequestOptions = {
  method?: string;
  body?: unknown;
  requireAntiforgery?: boolean;
  signal?: AbortSignal;
};

let antiforgeryTokenPromise: Promise<string> | null = null;

async function readErrorMessage(response: Response): Promise<string | undefined> {
  try {
    const data = (await response.json()) as { error?: string };
    return typeof data.error === "string" ? data.error : undefined;
  } catch {
    return undefined;
  }
}

export async function fetchAntiforgeryToken(signal?: AbortSignal): Promise<string> {
  const response = await fetch(ANTIFORGERY_PATH, {
    method: "GET",
    credentials: "same-origin",
    signal
  });

  if (!response.ok) {
    throw new ApiError(
      response.status,
      mapStatusToCode(response.status),
      "Não foi possível obter o token de proteção."
    );
  }

  const payload = (await response.json()) as AntiforgeryResponse;
  if (!payload.token || payload.token.length === 0) {
    throw new ApiError(400, "bad_request", "Token de proteção em falta.");
  }

  return payload.token;
}

async function resolveAntiforgeryToken(signal?: AbortSignal): Promise<string> {
  if (!antiforgeryTokenPromise) {
    antiforgeryTokenPromise = fetchAntiforgeryToken(signal).finally(() => {
      antiforgeryTokenPromise = null;
    });
  }

  return antiforgeryTokenPromise;
}

/** Clears any in-flight antiforgery cache (tests / after auth changes). */
export function resetAntiforgeryCache(): void {
  antiforgeryTokenPromise = null;
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers({
    Accept: "application/json"
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.requireAntiforgery) {
    const token = await resolveAntiforgeryToken(options.signal);
    headers.set(ANTIFORGERY_HEADER, token);
  }

  let response: Response;
  try {
    response = await fetch(path, {
      method: options.method ?? (options.body !== undefined ? "POST" : "GET"),
      credentials: "same-origin",
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal
    });
  } catch {
    throw new ApiError(0, "network", "Falha de comunicação com o servidor.");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    const bodyError = await readErrorMessage(response);
    throw new ApiError(
      response.status,
      mapStatusToCode(response.status),
      bodyError ?? "Pedido rejeitado pelo servidor.",
      bodyError
    );
  }

  if (response.status === 200 && response.headers.get("content-length") === "0") {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}
