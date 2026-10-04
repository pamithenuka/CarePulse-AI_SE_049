import axios from "axios";

const apiClient = axios.create({
  timeout: 150000,
  baseURL: process.env.REACT_APP_API_BASE_URL || "http://localhost:5014/api/v1",
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("carepulse_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("carepulse_token");
      localStorage.removeItem("carepulse_user");
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }
    const body = error.response?.data;
    if (typeof body === 'string') error.message = body;
    else if (body?.message) error.message = body.message;
    else if (body?.errors) error.message = Object.values(body.errors).flat().join(' ');
    return Promise.reject(error);
  }
);

export default apiClient;
