import apiClient from "./apiClient";

// Admin-only: onboards a doctor (login account + minimal profile) entirely
// from the web app, mirroring registerPatient's pattern.
export const registerDoctor = async (payload) => {
  const { data } = await apiClient.post("/doctors/register", payload);
  return data;
};

export const registerNurse = async (payload) => {
  const { data } = await apiClient.post("/nurses/register", payload);
  return data;
};

// Read-only directory, mirroring the Patient Master Registry pattern.
// status: "active" (default), "inactive" (deleted only) or "all" - Admin only.
export const getDoctors = async (status = "active") => {
  const { data } = await apiClient.get("/staff/doctors", { params: { status } });
  return data;
};

export const getDoctor = async (id) => {
  const { data } = await apiClient.get(`/staff/doctors/${id}`);
  return data;
};

export const getNurses = async (status = "active") => {
  const { data } = await apiClient.get("/staff/nurses", { params: { status } });
  return data;
};

export const getNurse = async (id) => {
  const { data } = await apiClient.get(`/staff/nurses/${id}`);
  return data;
};

// Admin-only soft delete: hides the record from the active directory and
// blocks login, without erasing it - mirrors deactivatePatient/reactivatePatient.
export const deleteDoctor = async (id) => {
  await apiClient.delete(`/doctors/${id}`);
};

export const restoreDoctor = async (id) => {
  await apiClient.put(`/doctors/${id}/restore`);
};

export const deleteNurse = async (id) => {
  await apiClient.delete(`/nurses/${id}`);
};

export const restoreNurse = async (id) => {
  await apiClient.put(`/nurses/${id}/restore`);
};
