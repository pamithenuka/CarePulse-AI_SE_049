import { create } from 'zustand';
import apiClient from '../api/apiClient';

export const useDispatchStore = create((set) => ({
  activeDispatches: [],
  availableNurses: [],
  isLoading: false,
  error: null,
  
  fetchDispatchData: async () => {
    set({ isLoading: true, error: null });
    try {
      // 1. Fetch real active dispatches from the backend
      const dispatchRes = await apiClient.get('/dispatch/active');
      const dispatches = dispatchRes.data.tickets || [];

      // 2. Fetch real nurses from the backend
      const nursesRes = await apiClient.get('/staff/nurses');
      const allNurses = nursesRes.data || [];
      
      // Filter out nurses that are currently on a dispatch
      const activeNurseIds = new Set(dispatches.map(d => d.nurseId));
      const availableNurses = allNurses.filter(nurse => !activeNurseIds.has(nurse.id));
      
      // Map the real dispatch tickets so the frontend components understand them
      const mappedDispatches = dispatches.map(d => {
        const assignedNurse = allNurses.find(n => n.id === d.nurseId);
        return {
          id: d.id,
          nurseId: d.nurseId,
          nurseName: assignedNurse ? assignedNurse.fullName : 'Unknown Nurse',
          status: d.status,
          assignedAt: d.assignedAt,
          location: { lat: 40.7128, lng: -74.0060 }, // Defaulting to NYC for the map UI if no route logs exist
          destination: { lat: 40.7128, lng: -74.0060 }
        };
      });

      // Map the real available nurses so the frontend understands them
      const mappedNurses = availableNurses.map(n => ({
        id: n.id,
        name: n.fullName,
        isAvailable: true,
        location: { lat: 40.7128, lng: -74.0060 }
      }));
      
      set({ 
        activeDispatches: mappedDispatches,
        availableNurses: mappedNurses,
        isLoading: false
      });
    } catch (err) {
      console.error("Failed to fetch dispatch data:", err);
      set({ error: err.message || "Failed to load data", isLoading: false });
    }
  }
}));
