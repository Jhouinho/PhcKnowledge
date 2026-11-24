export type ApiErrorCode =
  | "bad_request"
  | "unauthorized"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "network"
  | "unknown";

export class ApiError extends Error {
  readonly status: number;
  readonly code: ApiErrorCode;
  readonly bodyError?: string;

  constructor(status: number, code: ApiErrorCode, message: string, bodyError?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.bodyError = bodyError;
  }
}

export function mapStatusToCode(status: number): ApiErrorCode {
  switch (status) {
    case 400:
      return "bad_request";
    case 401:
      return "unauthorized";
    case 403:
      return "forbidden";
    case 404:
      return "not_found";
    case 409:
      return "conflict";
    default:
      return "unknown";
  }
}

export function userMessageForApiError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Não foi possível concluir o pedido. Tente novamente.";
  }

  if (error.bodyError && error.bodyError.trim().length > 0) {
    return error.bodyError;
  }

  switch (error.code) {
    case "bad_request":
      return "O pedido não é válido.";
    case "unauthorized":
      return "A sessão expirou. Inicie sessão novamente.";
    case "forbidden":
      return "Não tem permissão para esta operação.";
    case "not_found":
      return "A análise pedida não foi encontrada.";
    case "conflict":
      return "Não foi possível registar a decisão devido a um conflito.";
    case "network":
      return "Falha de comunicação com o servidor.";
    default:
      return "Não foi possível concluir o pedido. Tente novamente.";
  }
}
