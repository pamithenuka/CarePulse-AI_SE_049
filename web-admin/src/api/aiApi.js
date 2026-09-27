import apiClient from "./apiClient";

// Agent 1 (Coordinator/Planner) — turns a domain objective into a structured plan.

export const createAiPlan = async (patientId, objective) => {
  const { data } = await apiClient.post(`/patients/${patientId}/ai-plan`, { objective });
  return data;
};

export const getAiPlans = async (patientId) => {
  const { data } = await apiClient.get(`/patients/${patientId}/ai-plan`);
  return data;
};

export const reviewAiPlan = async (patientId, workflowId, payload) => {
  const { data } = await apiClient.put(`/patients/${patientId}/ai-plan/${workflowId}/review`, payload);
  return data;
};
