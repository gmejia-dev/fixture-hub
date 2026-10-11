export type FieldErrors = Record<string, string[]>;

export type Problem = {
  status: number;
  errorCode: string;
  metadata: Record<string, unknown>;
  fieldErrors?: FieldErrors;
  traceId?: string;
};

export const networkErrorCode = "Network.Unavailable";

export const unauthenticatedErrorCode = "Auth.Unauthenticated";

export const malformedRequestErrorCode = "Request.Malformed";

export const validationFailedErrorCode = "Validation.Failed";

export function toProblem(status: number, body: unknown, traceIdHeader: string | null): Problem {
  const data = isRecord(body) ? body : {};
  const metadata = isRecord(data.metadata) ? data.metadata : {};

  return {
    status,
    errorCode: typeof data.errorCode === "string" ? data.errorCode : "",
    metadata,
    fieldErrors: isFieldErrors(metadata.errors) ? metadata.errors : undefined,
    traceId: typeof data.traceId === "string" ? data.traceId : (traceIdHeader ?? undefined),
  };
}

export function networkProblem(): Problem {
  return { status: 0, errorCode: networkErrorCode, metadata: {} };
}

export function clientProblem(status: number, errorCode: string): Problem {
  return { status, errorCode, metadata: {} };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isFieldErrors(value: unknown): value is FieldErrors {
  return (
    isRecord(value) &&
    Object.values(value).every(
      (codes) => Array.isArray(codes) && codes.every((code) => typeof code === "string"),
    )
  );
}
