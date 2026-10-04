import client from './apiClient';

async function request(path, options = {}) {
  const response = await client.request({ url: path, method: options.method || 'GET', data: options.body ? JSON.parse(options.body) : undefined });
  return response.data;
}

export const api = {
  getBooked: doctorId => request(`/appointments/booked?doctorId=${doctorId}`),
  getDoctors: () => request('/doctors'),

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
