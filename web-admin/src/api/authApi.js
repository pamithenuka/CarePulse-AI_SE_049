import apiClient from "./apiClient";

export const login = async (email, password) => {
  const { data } = await apiClient.post("/auth/login", { email, password });
  return data;
};

export const register = async (payload) => {
  const { data } = await apiClient.post("/auth/register", payload);
  return data;
};
