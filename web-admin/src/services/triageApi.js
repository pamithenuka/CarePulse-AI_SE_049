const API_BASE_URL = 'http://localhost:5014/api/v1/triage';

export const triageApi = {
  submitTriage: async (patientId, symptoms) => {
    const response = await fetch(`${API_BASE_URL}/submit`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ patientId, symptoms }),
    });
    if (!response.ok) throw new Error('Failed to submit triage');
    return response.json();
  },

  getPendingApprovals: async () => {
    const response = await fetch(`${API_BASE_URL}/pending-approvals`);
    if (!response.ok) throw new Error('Failed to fetch pending approvals');
    return response.json();
  },

  getAuditLog: async (id) => {
    const response = await fetch(`${API_BASE_URL}/${id}/audit-log`);
    if (!response.ok) throw new Error('Failed to fetch audit log');
    return response.json();
  },

  approveTriage: async (id, notes) => {
    const response = await fetch(`${API_BASE_URL}/${id}/approve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes }),
    });
    if (!response.ok) throw new Error('Failed to approve triage');
    return response.json();
  },

  rejectTriage: async (id, notes) => {
    const response = await fetch(`${API_BASE_URL}/${id}/reject`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes }),
    });
    if (!response.ok) {
      let detail = 'Failed to reject triage';
      try {
        const body = await response.json();
        detail = body.message || body.title || detail;
      } catch {
        // keep default message when the body is not JSON
      }
      throw new Error(detail);
    }
    return response.json();
  },

  deleteTriage: async (id) => {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error('Failed to delete triage');
    return true;
  }
};
