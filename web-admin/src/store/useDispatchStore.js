import { create } from 'zustand';

// Simulated API calls for demonstration
const mockFetchActiveDispatches = async () => {
  return [
    {
      id: '1',
      nurseId: 'n1',
      nurseName: 'Sarah Jenkins',
      status: 'EnRoute',
      assignedAt: new Date().toISOString(),
      location: { lat: 40.7128, lng: -74.0060 },
      destination: { lat: 40.7300, lng: -73.9900 }
    },
    {
      id: '2',
      nurseId: 'n2',
      nurseName: 'Michael Chang',
      status: 'ArrivedOnSite',
      assignedAt: new Date(Date.now() - 3600000).toISOString(),
      location: { lat: 40.7580, lng: -73.9855 },
      destination: { lat: 40.7580, lng: -73.9855 }
    }
  ];
};

const mockFetchAvailableNurses = async () => {
  return [
    { id: 'n3', name: 'Emma Watson', isAvailable: true, location: { lat: 40.7306, lng: -73.9352 } },
    { id: 'n4', name: 'David Lee', isAvailable: true, location: { lat: 40.7488, lng: -73.9680 } }
  ];
};

export const useDispatchStore = create((set) => ({
  activeDispatches: [],
  availableNurses: [],
  isLoading: false,
  error: null,
  
  fetchDispatchData: async () => {
    set({ isLoading: true });
    try {
      // In a real app, this would call our ASP.NET Core backend:
      // const res = await fetch('/api/v1/dispatch/active');
      // const data = await res.json();
      
      const dispatches = await mockFetchActiveDispatches();
      const nurses = await mockFetchAvailableNurses();
      
      set({ 
        activeDispatches: dispatches,
        availableNurses: nurses,
        isLoading: false,
        error: null
      });
    } catch (err) {
      set({ error: err.message, isLoading: false });
    }
  }
}));
