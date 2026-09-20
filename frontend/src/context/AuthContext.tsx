'use client';

import { createContext, useContext, useEffect, useState, ReactNode, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api';
import { getToken, saveToken, clearToken } from '@/lib/auth-cookies';
import { isTokenExpired } from '@/lib/jwt';
import type { User } from '@/lib/types';

interface AuthContextValue {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<User>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  // Khi app khởi động (F5 lại trang), nếu còn cookie hợp lệ thì gọi /me để khôi phục state user.
  // Không tự suy ra user từ payload JWT để tránh hiển thị sai nếu tài khoản vừa bị Admin khóa.
  useEffect(() => {
    const token = getToken();

    if (!token || isTokenExpired(token)) {
      clearToken();
      setIsLoading(false);
      return;
    }

    authApi
      .getMe()
      .then((res) => setUser(res.data.data))
      .catch(() => {
        clearToken();
        setUser(null);
      })
      .finally(() => setIsLoading(false));
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login(email, password);
    const loginData = res.data.data;

    if (!loginData) {
      throw new Error('Phản hồi đăng nhập không hợp lệ.');
    }

    saveToken(loginData.accessToken);
    setUser(loginData.user);
    
    return loginData.user;
  }, []);

  const logout = useCallback(() => {
    clearToken();
    setUser(null);
    router.push('/login');
  }, [router]);

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth phải được dùng bên trong <AuthProvider>');
  return ctx;
}
