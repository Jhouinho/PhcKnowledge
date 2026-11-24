import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AnalysisDecisionPanel from "./AnalysisDecisionPanel";
import type {
  AnalysisDecisionHistory,
  AnalysisDecisionState,
  AnalysisDecisionWriteResponse
} from "./analysisDecisionTypes";
import { ANTIFORGERY_HEADER, ANTIFORGERY_PATH, resetAntiforgeryCache } from "../../lib/api/httpClient";

const analysisId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

function stateFixture(
  overrides: Partial<AnalysisDecisionState> = {}
): AnalysisDecisionState {
  return {
    analysisId,
    packageId: "11111111-1111-1111-1111-111111111111",
    decision: "unconfirmed",
    decisionSequence: null,
    decidedAtUtc: null,
    decidedBySubject: null,
    decidedByIssuer: null,
    rationale: null,
    semanticFingerprintSnapshot: null,
    materializationReadiness: "Ready",
    ...overrides
  };
}

function historyFixture(
  items: AnalysisDecisionHistory["items"] = []
): AnalysisDecisionHistory {
  return {
    analysisId,
    packageId: "11111111-1111-1111-1111-111111111111",
    items
  };
}

function renderPanel() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  });

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AnalysisDecisionPanel analysisId={analysisId} />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

function mockApi(handlers: {
  state: AnalysisDecisionState | (() => AnalysisDecisionState);
  history: AnalysisDecisionHistory | (() => AnalysisDecisionHistory);
  post?: (body: unknown) => Response | Promise<Response>;
}) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();

    if (url === ANTIFORGERY_PATH) {
      return new Response(JSON.stringify({ token: "test-csrf" }), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    }

    if (url.endsWith(`/decision`) && method === "GET") {
      const payload = typeof handlers.state === "function" ? handlers.state() : handlers.state;
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    }

    if (url.endsWith(`/decisions`) && method === "GET") {
      const payload = typeof handlers.history === "function" ? handlers.history() : handlers.history;
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    }

    if (url.endsWith(`/decisions`) && method === "POST") {
      if (handlers.post) {
        return handlers.post(init?.body ? JSON.parse(String(init.body)) : null);
      }

      const write: AnalysisDecisionWriteResponse = {
        analysisId,
        packageId: "11111111-1111-1111-1111-111111111111",
        decision: "confirmed",
        decisionSequence: 1,
        decidedAtUtc: "2026-11-19T12:00:00Z",
        decidedBySubject: "22222222-2222-2222-2222-222222222222",
        decidedByIssuer: "phcknowledge.portal",
        rationale: null,
        semanticFingerprintSnapshot: "a".repeat(64),
        idempotentReplay: false
      };
      return new Response(JSON.stringify(write), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    }

    return new Response(JSON.stringify({ error: `Unhandled ${method} ${url}` }), {
      status: 500,
      headers: { "Content-Type": "application/json" }
    });
  });

  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("AnalysisDecisionPanel", () => {
  beforeEach(() => {
    resetAntiforgeryCache();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    resetAntiforgeryCache();
  });

  it("shows Unconfirmed state", async () => {
    mockApi({
      state: stateFixture({ decision: "unconfirmed" }),
      history: historyFixture()
    });

    renderPanel();

    expect(await screen.findByText("Unconfirmed")).toBeInTheDocument();
    expect(
      screen.getByText(/Ainda não existe uma decisão humana registada/i)
    ).toBeInTheDocument();
  });

  it("shows Confirmed state details from the API only", async () => {
    mockApi({
      state: stateFixture({
        decision: "confirmed",
        decisionSequence: 2,
        decidedAtUtc: "2026-11-19T15:30:00Z",
        decidedBySubject: "subject-1",
        decidedByIssuer: "phcknowledge.portal",
        rationale: "ok"
      }),
      history: historyFixture()
    });

    renderPanel();

    expect(await screen.findByText("Confirmed")).toBeInTheDocument();
    expect(screen.getByText("subject-1")).toBeInTheDocument();
    expect(screen.getByText("phcknowledge.portal")).toBeInTheDocument();
    expect(screen.getByText("ok")).toBeInTheDocument();
  });

  it("shows Rejected state", async () => {
    mockApi({
      state: stateFixture({
        decision: "rejected",
        decisionSequence: 1,
        decidedAtUtc: "2026-11-19T15:30:00Z",
        decidedBySubject: "subject-2",
        decidedByIssuer: "phcknowledge.portal",
        rationale: "dados incompletos"
      }),
      history: historyFixture()
    });

    renderPanel();

    expect(await screen.findByText("Rejected")).toBeInTheDocument();
    expect(screen.getByText("dados incompletos")).toBeInTheDocument();
  });

  it("shows empty history state", async () => {
    mockApi({
      state: stateFixture(),
      history: historyFixture([])
    });

    renderPanel();

    expect(await screen.findByText("Sem histórico de decisões.")).toBeInTheDocument();
  });

  it("shows history with multiple decisions and marks the effective one", async () => {
    mockApi({
      state: stateFixture({
        decision: "confirmed",
        decisionSequence: 2,
        decidedAtUtc: "2026-11-19T16:00:00Z",
        decidedBySubject: "subject-1",
        decidedByIssuer: "phcknowledge.portal"
      }),
      history: historyFixture([
        {
          decisionId: "d2",
          decision: "confirmed",
          decisionSequence: 2,
          decidedAtUtc: "2026-11-19T16:00:00Z",
          decidedBySubject: "subject-1",
          decidedByIssuer: "phcknowledge.portal",
          rationale: null,
          semanticFingerprintSnapshot: "a".repeat(64)
        },
        {
          decisionId: "d1",
          decision: "rejected",
          decisionSequence: 1,
          decidedAtUtc: "2026-11-19T15:00:00Z",
          decidedBySubject: "subject-0",
          decidedByIssuer: "phcknowledge.portal",
          rationale: "primeira",
          semanticFingerprintSnapshot: "a".repeat(64)
        }
      ])
    });

    renderPanel();

    expect(await screen.findByText("Efetiva")).toBeInTheDocument();
    expect(screen.getByText("#2")).toBeInTheDocument();
    expect(screen.getByText("#1")).toBeInTheDocument();
    expect(screen.getByText("primeira")).toBeInTheDocument();
  });

  it("confirms with optional rationale and refreshes state/history", async () => {
    let current = stateFixture({ decision: "unconfirmed" });
    let history = historyFixture([]);

    const fetchMock = mockApi({
      state: () => current,
      history: () => history,
      post: async (body) => {
        const rationale =
          body && typeof body === "object" && "rationale" in body
            ? String((body as { rationale?: string }).rationale ?? "")
            : "";
        current = stateFixture({
          decision: "confirmed",
          decisionSequence: 1,
          decidedAtUtc: "2026-11-19T12:00:00Z",
          decidedBySubject: "subject-1",
          decidedByIssuer: "phcknowledge.portal",
          rationale: rationale.length > 0 ? rationale : "opcional"
        });
        history = historyFixture([
          {
            decisionId: "d1",
            decision: "confirmed",
            decisionSequence: 1,
            decidedAtUtc: "2026-11-19T12:00:00Z",
            decidedBySubject: "subject-1",
            decidedByIssuer: "phcknowledge.portal",
            rationale: current.rationale,
            semanticFingerprintSnapshot: "a".repeat(64)
          }
        ]);

        return new Response(
          JSON.stringify({
            analysisId,
            packageId: current.packageId,
            decision: "confirmed",
            decisionSequence: 1,
            decidedAtUtc: "2026-11-19T12:00:00Z",
            decidedBySubject: "subject-1",
            decidedByIssuer: "phcknowledge.portal",
            rationale: current.rationale,
            semanticFingerprintSnapshot: "a".repeat(64),
            idempotentReplay: false
          }),
          { status: 200, headers: { "Content-Type": "application/json" } }
        );
      }
    });

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText("Unconfirmed");
    await user.click(screen.getByRole("button", { name: "Confirmar análise" }));
    const rationaleInput = screen.getByLabelText(/Justificação/i);
    fireEvent.change(rationaleInput, { target: { value: "opcional" } });
    await user.click(screen.getByRole("button", { name: "Confirmar decisão" }));

    await waitFor(() => {
      expect(screen.getAllByText("Confirmed").length).toBeGreaterThan(0);
    });
    expect(screen.getAllByText("opcional").length).toBeGreaterThan(0);

    const postCall = fetchMock.mock.calls.find(
      ([url, init]) =>
        String(url).endsWith("/decisions") &&
        (init?.method ?? "GET").toUpperCase() === "POST"
    );
    expect(postCall).toBeTruthy();
    const headers = new Headers(postCall![1]?.headers);
    expect(headers.get(ANTIFORGERY_HEADER)).toBe("test-csrf");
    expect(JSON.parse(String(postCall![1]?.body))).toEqual({
      decision: "confirmed",
      rationale: "opcional"
    });
  });

  it("requires non-whitespace rationale for rejection", async () => {
    mockApi({
      state: stateFixture(),
      history: historyFixture()
    });

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText("Unconfirmed");
    await user.click(screen.getByRole("button", { name: "Rejeitar análise" }));

    const submit = screen.getByRole("button", { name: "Confirmar rejeição" });
    expect(submit).toBeDisabled();

    await user.type(screen.getByLabelText(/Justificação/i), "   ");
    expect(submit).toBeDisabled();
  });

  it("hides confirm for Blocked analyses", async () => {
    mockApi({
      state: stateFixture({ materializationReadiness: "Blocked" }),
      history: historyFixture()
    });

    renderPanel();

    await screen.findByText(/apenas a rejeição está disponível/i);
    expect(screen.getByRole("button", { name: "Confirmar análise" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Rejeitar análise" })).toBeEnabled();
  });

  it("disables actions while a submission is in progress", async () => {
    let resolvePost: (value: Response) => void = () => undefined;

    mockApi({
      state: stateFixture(),
      history: historyFixture(),
      post: () =>
        new Promise<Response>((resolve) => {
          resolvePost = resolve;
        })
    });

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText("Unconfirmed");
    await user.click(screen.getByRole("button", { name: "Confirmar análise" }));
    await user.click(screen.getByRole("button", { name: "Confirmar decisão" }));

    expect(await screen.findByRole("button", { name: "A submeter…" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Confirmar análise" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Rejeitar análise" })).toBeDisabled();

    resolvePost(
      new Response(
        JSON.stringify({
          analysisId,
          packageId: "11111111-1111-1111-1111-111111111111",
          decision: "confirmed",
          decisionSequence: 1,
          decidedAtUtc: "2026-11-19T12:00:00Z",
          decidedBySubject: "subject-1",
          decidedByIssuer: "phcknowledge.portal",
          rationale: null,
          semanticFingerprintSnapshot: "a".repeat(64),
          idempotentReplay: false
        }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      )
    );
  });

  it.each([
    [400, "Pedido inválido."],
    [401, "A sessão expirou. Inicie sessão novamente."],
    [403, "Não tem permissão para esta operação."],
    [404, "A análise pedida não foi encontrada."],
    [409, "A análise não é elegível para esta decisão."]
  ] as const)("surfaces HTTP %s errors understandably", async (status, message) => {
    mockApi({
      state: stateFixture(),
      history: historyFixture(),
      post: async () =>
        new Response(JSON.stringify({ error: message }), {
          status,
          headers: { "Content-Type": "application/json" }
        })
    });

    const user = userEvent.setup();
    renderPanel();

    await screen.findByText("Unconfirmed");
    await user.click(screen.getByRole("button", { name: "Confirmar análise" }));
    await user.click(screen.getByRole("button", { name: "Confirmar decisão" }));

    const dialog = await screen.findByRole("dialog");
    expect(await within(dialog).findByRole("alert")).toHaveTextContent(message);
  });
});
