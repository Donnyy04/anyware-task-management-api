import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, clearSession, getAccessToken, saveSession, setUnauthorizedHandler } from './api';
import { useQueryClient } from '@tanstack/react-query';
import type { AuthResponse, LoginRequest, RegisterRequest, User } from './types';

interface AuthValue { user: User | null; loading: boolean; message: string; login: (data: LoginRequest) => Promise<void>; register: (data: RegisterRequest) => Promise<void>; logout: (message?: string) => void; clearMessage: () => void }
const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const logout = (reason = '') => { clearSession(); queryClient.clear(); setUser(null); setMessage(reason); };
  useEffect(() => { setUnauthorizedHandler(() => logout('Your session expired. Please log in again.')); return () => setUnauthorizedHandler(null); }, []);
  useEffect(() => {
    let active = true;
    if (!getAccessToken()) { setLoading(false); return () => { active = false; }; }
    api.me().then(me => { if (active) setUser(me); }).catch(() => { if (active) setUser(null); }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);
  const finishAuth = async (auth: AuthResponse) => { saveSession(auth); setUser(await api.me()); setMessage(''); };
  const value = useMemo<AuthValue>(() => ({ user, loading, message, login: async data => finishAuth(await api.login(data)), register: async data => finishAuth(await api.register(data)), logout, clearMessage: () => setMessage('') }), [user, loading, message]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
export function useAuth(): AuthValue { const value = useContext(AuthContext); if (!value) throw new Error('useAuth must be used inside AuthProvider'); return value; }
