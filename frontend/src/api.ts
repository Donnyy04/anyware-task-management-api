import type { AuthResponse, CreateTaskRequest, LoginRequest, RegisterRequest, TaskItem, UpdateTaskStatusRequest, User } from './types';

const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5154/api').replace(/\/$/, '');
const ACCESS = 'anyware.access';
const REFRESH = 'anyware.refresh';
let refreshInFlight: Promise<boolean> | null = null;
let onUnauthorized: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void { onUnauthorized = handler; }
export function getAccessToken(): string | null { return localStorage.getItem(ACCESS); }
export function clearSession(): void { localStorage.removeItem(ACCESS); localStorage.removeItem(REFRESH); }
export function saveSession(auth: AuthResponse): void { localStorage.setItem(ACCESS, auth.accessToken); localStorage.setItem(REFRESH, auth.refreshToken); }

export class ApiError extends Error {
  constructor(message: string, public readonly status: number) { super(message); this.name = 'ApiError'; }
}

async function refreshSession(): Promise<boolean> {
  const refreshToken = localStorage.getItem(REFRESH);
  if (!refreshToken) return false;
  if (!refreshInFlight) refreshInFlight = fetch(`${API_URL}/auth/refresh`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken }) })
    .then(async response => { if (!response.ok) return false; saveSession(await response.json() as AuthResponse); return true; })
    .catch(() => false).finally(() => { refreshInFlight = null; });
  return refreshInFlight;
}

async function request<T>(path: string, init: RequestInit = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  let response: Response;
  try { response = await fetch(`${API_URL}${path}`, { ...init, headers }); }
  catch { throw new ApiError('Unable to reach the API. Check that the backend is running.', 0); }
  if (response.status === 401 && retry && !path.startsWith('/auth/')) {
    if (await refreshSession()) return request<T>(path, init, false);
  }
  if (response.status === 401) { clearSession(); onUnauthorized?.(); }
  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try { const body = await response.json() as { detail?: string; message?: string; title?: string }; message = body.detail ?? body.message ?? body.title ?? message; } catch { /* use status fallback */ }
    throw new ApiError(message, response.status);
  }
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

export const api = {
  register: (data: RegisterRequest) => request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(data) }),
  login: (data: LoginRequest) => request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(data) }),
  me: () => request<User>('/auth/me'),
  listTasks: () => request<TaskItem[]>('/tasks'),
  getTask: (id: string) => request<TaskItem>(`/tasks/${id}`),
  createTask: (data: CreateTaskRequest) => request<TaskItem>('/tasks', { method: 'POST', body: JSON.stringify(data) }),
  updateTaskStatus: (id: string, data: UpdateTaskStatusRequest) => request<TaskItem>(`/tasks/${id}/status`, { method: 'PATCH', body: JSON.stringify(data) }),
  listUsers: () => request<User[]>('/admin/users'),
  createUser: (data: RegisterRequest) => request<User>('/admin/users', { method: 'POST', body: JSON.stringify(data) }),
  deleteUser: (id: string) => request<void>(`/admin/users/${id}`, { method: 'DELETE' }),
};
