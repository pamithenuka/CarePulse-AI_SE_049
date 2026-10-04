import { create } from 'zustand';
import apiClient from '../api/apiClient';
export const useDispatchStore = create((set, get) => ({
  activeDispatches: [], availableNurses: [], waitingCases: [], isLoading: false, error: null,
  fetchDispatchData: async () => {
    if (get().isLoading) return;
    set({ isLoading: true, error: null });
    try {
      const dispatches = [];
      let page = 1, total = 0;
      do {
        const { data } = await apiClient.get('/dispatch/active', { params: { page, pageSize: 100 } });
        dispatches.push(...data.tickets); total = data.totalItems; page++;
        if (data.tickets.length === 0) break;
      } while (dispatches.length < total);
      const [nurses, waiting] = await Promise.all([apiClient.get('/dispatch/nurses'), apiClient.get('/dispatch/waiting')]);
      set({ activeDispatches: dispatches.map(d => ({ ...d,
        location: d.locationRecordedAt ? { lat: d.currentLat, lng: d.currentLng } : null,
        destination: d.destinationLat != null && d.destinationLng != null ? { lat: d.destinationLat, lng: d.destinationLng } : null
      })), availableNurses: nurses.data.filter(n => n.isAvailable).map(n => ({ ...n, name: n.fullName })),
      waitingCases: waiting.data, isLoading: false });
    } catch (error) { set({ error: error.message, isLoading: false }); }
  }
}));
