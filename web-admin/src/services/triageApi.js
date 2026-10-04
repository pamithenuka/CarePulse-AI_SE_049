import client from '../api/apiClient';
export const triageApi = {
  submitTriage: async (patientProfileId, symptoms) => (await client.post('/triage/submit', { patientProfileId, symptoms })).data,
  getPendingApprovals: async () => (await client.get('/triage/pending-approvals')).data,
  getAuditLog: async id => (await client.get(`/triage/${id}/audit-log`)).data,
  getStatus: async id => (await client.get(`/triage/${id}`)).data,
  approveTriage: async (id, notes) => (await client.post(`/triage/${id}/approve`, { notes })).data,
  rejectTriage: async (id, notes) => (await client.post(`/triage/${id}/reject`, { notes })).data,
  deleteTriage: async id => { await client.delete(`/triage/${id}`); return true; }
};
