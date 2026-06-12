export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
  }
}

const baseUrl: string = import.meta.env.VITE_API_URL ?? '/api';

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response
      .json()
      .catch(() => ({ title: response.statusText }));
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
