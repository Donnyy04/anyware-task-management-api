import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api, clearSession, saveSession, setUnauthorizedHandler } from '../src/api';

function response(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: vi.fn().mockResolvedValue(body),
  } as unknown as Response;
}

describe('API client', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    clearSession();
    setUnauthorizedHandler(null);
  });

  it('adds the saved access token to authenticated requests', async () => {
    const user = { id: 'user-1', name: 'Donny', email: 'donny@example.com', role: 'User', createdAt: '2026-10-05T00:00:00Z' };
    const fetchMock = vi.fn().mockResolvedValue(response(200, user));
    vi.stubGlobal('fetch', fetchMock);
    localStorage.setItem('anyware.access', 'access-token');

    await expect(api.me()).resolves.toEqual(user);

    const headers = new Headers(fetchMock.mock.calls[0][1]?.headers);
    expect(headers.get('Authorization')).toBe('Bearer access-token');
  });

  it('refreshes an expired access token once and retries the request', async () => {
    const auth = { accessToken: 'new-access-token', refreshToken: 'new-refresh-token', accessTokenExpiresAt: '2026-10-05T01:00:00Z' };
    const page = { items: [], pageNumber: 1, pageSize: 10, totalCount: 0 };
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(response(401, { title: 'Unauthorized' }))
      .mockResolvedValueOnce(response(200, auth))
      .mockResolvedValueOnce(response(200, page));
    vi.stubGlobal('fetch', fetchMock);
    saveSession({ accessToken: 'old-access-token', refreshToken: 'old-refresh-token', accessTokenExpiresAt: '2026-10-05T00:00:00Z' });

    await expect(api.listTasks(1, 'All', '')).resolves.toEqual(page);

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls[1][0]).toBe('http://localhost:5154/api/auth/refresh');
    expect(JSON.parse(String(fetchMock.mock.calls[1][1]?.body))).toEqual({ refreshToken: 'old-refresh-token' });
    const retryHeaders = new Headers(fetchMock.mock.calls[2][1]?.headers);
    expect(retryHeaders.get('Authorization')).toBe('Bearer new-access-token');
    expect(localStorage.getItem('anyware.refresh')).toBe('new-refresh-token');
  });
});
