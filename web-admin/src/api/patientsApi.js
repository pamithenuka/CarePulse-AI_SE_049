import apiClient from "./apiClient";

// ---------- Profile ----------

export const getPatients = async ({ search, bloodGroup, condition, minAge, maxAge, status, sortBy, descending, page, pageSize }) => {
  const { data } = await apiClient.get("/patients", {
    params: { search, bloodGroup, condition, minAge, maxAge, status, sortBy, descending, page, pageSize },
  });
  return data;
};

export const getPatientProfile = async (id) => {
  const { data } = await apiClient.get(`/patients/${id}`);
  return data;
};

// Admin-only: onboards a patient (login account + profile) entirely from the
// web app, so front-desk staff never need the mobile app to register someone.
export const registerPatient = async (payload) => {
  const { data } = await apiClient.post("/patients/register", payload);
  return data;
};

export const updatePatientProfile = async (id, payload) => {
  const { data } = await apiClient.put(`/patients/${id}`, payload);
  return data;
};

export const deactivatePatient = async (id) => {
  await apiClient.delete(`/patients/${id}`);
};

export const reactivatePatient = async (id) => {
  await apiClient.put(`/patients/${id}/reactivate`);
};

// ---------- Medical history ----------

export const getMedicalHistory = async (id) => {
  const { data } = await apiClient.get(`/patients/${id}/history`);
  return data;
};

export const addMedicalHistory = async (id, payload) => {
  const { data } = await apiClient.post(`/patients/${id}/history`, payload);
  return data;
};

export const updateMedicalHistory = async (id, historyId, payload) => {
  const { data } = await apiClient.put(`/patients/${id}/history/${historyId}`, payload);
  return data;
};

export const deleteMedicalHistory = async (id, historyId) => {
  await apiClient.delete(`/patients/${id}/history/${historyId}`);
};

// ---------- Emergency contacts ----------

export const getEmergencyContacts = async (id) => {
  const { data } = await apiClient.get(`/patients/${id}/emergency-contacts`);
  return data;
};

export const addEmergencyContact = async (id, payload) => {
  const { data } = await apiClient.post(`/patients/${id}/emergency-contacts`, payload);
  return data;
};

export const updateEmergencyContact = async (id, contactId, payload) => {
  const { data } = await apiClient.put(`/patients/${id}/emergency-contacts/${contactId}`, payload);
  return data;
};

export const deleteEmergencyContact = async (id, contactId) => {
  await apiClient.delete(`/patients/${id}/emergency-contacts/${contactId}`);
};

// ---------- Documents ----------

export const getDocuments = async (id) => {
  const { data } = await apiClient.get(`/patients/${id}/documents`);
  return data;
};

export const uploadDocument = async (id, file, documentType) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("documentType", documentType);
  const { data } = await apiClient.post(`/patients/${id}/documents`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
};

export const updateDocument = async (id, documentId, payload) => {
  const { data } = await apiClient.put(`/patients/${id}/documents/${documentId}`, payload);
  return data;
};

export const deleteDocument = async (id, documentId) => {
  await apiClient.delete(`/patients/${id}/documents/${documentId}`);
};

// Downloads require the Authorization header, so a plain <a href> won't work —
// fetch the bytes through axios (which attaches the JWT) and hand back a blob URL.
export const fetchDocumentBlobUrl = async (id, documentId) => {
  const response = await apiClient.get(`/patients/${id}/documents/${documentId}/download`, {
    responseType: "blob",
  });
  return URL.createObjectURL(response.data);
};

// ---------- Audit log ----------

export const getPatientAuditLog = async (id, { page, pageSize } = {}) => {
  const { data } = await apiClient.get(`/patients/${id}/audit-log`, { params: { page, pageSize } });
  return data;
};

export const getAuditLog = async ({ patientId, userId, from, to, page, pageSize }) => {
  const { data } = await apiClient.get("/patients/audit-log", {
    params: { patientId, userId, from, to, page, pageSize },
  });
  return data;
};

// ---------- Emergency alert ----------

export const triggerEmergencyAlert = async (id) => {
  const { data } = await apiClient.post(`/patients/${id}/emergency-alert`);
  return data;
};

export const getEmergencyAlertLogs = async ({ page, pageSize } = {}) => {
  const { data } = await apiClient.get("/patients/emergency-alerts", { params: { page, pageSize } });
  return data;
};
