export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem?: ProblemDetails;

  public constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim();
export const apiBaseUrl = (configuredBaseUrl || 'http://localhost:5080').replace(/\/+$/, '');

type QueryValue = string | number | boolean | undefined | null;

export function withQuery<T extends object>(path: string, values: T): string {
  const query = new URLSearchParams();

  (Object.entries(values) as [string, QueryValue][]).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      query.set(key, String(value));
    }
  });

  const queryString = query.toString();
  return queryString.length > 0 ? `${path}?${queryString}` : path;
}

function problemMessage(problem: ProblemDetails | undefined, status: number): string {
  if (problem?.errors) {
    const validationMessage = Object.values(problem.errors).flat().find(Boolean);
    if (validationMessage) {
      return validationMessage;
    }
  }

  return problem?.detail || problem?.title || `The server returned status ${status}.`;
}

async function parseProblem(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) {
    return undefined;
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: 'GET',
      headers: { Accept: 'application/json' },
      signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }

    throw new ApiError(0, 'Unable to reach the clinic API. Check that the API is running.');
  }

  if (!response.ok) {
    const problem = await parseProblem(response);
    throw new ApiError(response.status, problemMessage(problem, response.status), problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
