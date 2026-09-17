import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { login as loginRequest } from "../api/authApi";

const AuthContext = createContext(null);

const readStoredUser = () => {
  try {
    const raw = localStorage.getItem("carepulse_user");
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser);

  const login = useCallback(async (email, password) => {
    const response = await loginRequest(email, password);
    const currentUser = {
      userId: response.userId,
      fullName: response.fullName,
      email: response.email,
      roles: response.roles,
    };
    localStorage.setItem("carepulse_token", response.token);
    localStorage.setItem("carepulse_user", JSON.stringify(currentUser));
    setUser(currentUser);
    return currentUser;
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem("carepulse_token");
    localStorage.removeItem("carepulse_user");
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (...roles) => !!user && roles.some((role) => user.roles?.includes(role)),
    [user]
  );

  const value = useMemo(
    () => ({ user, isAuthenticated: !!user, login, logout, hasRole }),
    [user, login, logout, hasRole]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
