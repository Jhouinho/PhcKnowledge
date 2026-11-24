import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, type RenderOptions } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import type { ReactElement, ReactNode } from "react";
import { AuthProvider } from "../features/auth/AuthProvider";

type Options = Omit<RenderOptions, "wrapper"> & {
  route?: string;
  withAuth?: boolean;
};

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 0 },
      mutations: { retry: false }
    }
  });
}

export function renderWithProviders(ui: ReactElement, options: Options = {}) {
  const queryClient = createTestQueryClient();
  const route = options.route ?? "/";
  const withAuth = options.withAuth ?? false;

  function Wrapper({ children }: { children: ReactNode }) {
    const tree = (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[route]}>
          {withAuth ? <AuthProvider>{children}</AuthProvider> : children}
        </MemoryRouter>
      </QueryClientProvider>
    );
    return tree;
  }

  return {
    queryClient,
    ...render(ui, { wrapper: Wrapper, ...options })
  };
}
