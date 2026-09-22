const BASE_URL = 'http://localhost:5014/api/v1';

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });

  let body = null;
  const text = await res.text();
  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      body = text;
    }
  }

  if (!res.ok) {
    const message =
      (body && (body.message || body.Message || body.title || body)) ||
      `Request failed with status ${res.status}`;
    const error = new Error(typeof message === 'string' ? message : JSON.stringify(message));
    error.status = res.status;
    error.body = body;
    throw error;
  }

  return body;
}

export const api = {
  getDoctors: () => request('/doctors'),

  createDoctor: (payload) => request('/doctors', { method: 'POST', body: JSON.stringify(payload) }),

  updateDoctor: (doctorId, payload) =>
    request(`/doctors/${doctorId}`, { method: 'PUT', body: JSON.stringify(payload) }),

  getRoster: (doctorId) => request(`/doctors/${doctorId}/roster`),

  updateRoster: (payload) =>
    request('/doctors/roster', { method: 'PUT', body: JSON.stringify(payload) }),

  generateSlots: (doctorId, payload) =>
    request(`/doctors/${doctorId}/generate-slots`, {
      method: 'POST',
      body: JSON.stringify(payload)
    }),

  getSlots: (params = {}) => {
    const query = new URLSearchParams(
      Object.fromEntries(Object.entries(params).filter(([, v]) => v))
    ).toString();
    return request(`/doctors/slots${query ? `?${query}` : ''}`);
  },

  bookAppointment: (payload) =>
    request('/appointments/book', { method: 'POST', body: JSON.stringify(payload) }),

  completeConsultation: (payload) =>
    request('/consultations/complete', { method: 'POST', body: JSON.stringify(payload) }),

  getConsultation: (id) => request(`/consultations/${id}`)
};
