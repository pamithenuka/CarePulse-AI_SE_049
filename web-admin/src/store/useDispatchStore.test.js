import api from '../api/apiClient';
import { useDispatchStore } from './useDispatchStore';

jest.mock('../api/apiClient', () => ({ get: jest.fn() }));

beforeEach(() => {
  jest.clearAllMocks();
  useDispatchStore.setState({ activeDispatches: [], availableNurses: [], waitingCases: [], isLoading: false, error: null });
});

test('loads every page and preserves real destination, telemetry, and server availability', async () => {
  api.get.mockImplementation((path, config) => {
    if (path === '/dispatch/active') return Promise.resolve({ data: {
      totalItems: 2,
      tickets: config.params.page === 1
        ? [{ id: 'd1', currentLat: 6.92, currentLng: 79.86, locationRecordedAt: '2026-09-28T10:00:00Z', destinationLat: 7.1, destinationLng: 80.2 }]
        : [{ id: 'd2', currentLat: 0, currentLng: 0, locationRecordedAt: null, destinationLat: null, destinationLng: null }],
    } });
    if (path === '/dispatch/nurses') return Promise.resolve({ data: [
      { id: 'n1', fullName: 'Available nurse', isAvailable: true },
      { id: 'n2', fullName: 'Busy nurse', isAvailable: false },
    ] });
    return Promise.resolve({ data: [{ id: 'waiting' }] });
  });
  await useDispatchStore.getState().fetchDispatchData();
  const state = useDispatchStore.getState();
  expect(state.activeDispatches).toHaveLength(2);
  expect(state.activeDispatches[0].location).toEqual({ lat: 6.92, lng: 79.86 });
  expect(state.activeDispatches[0].destination).toEqual({ lat: 7.1, lng: 80.2 });
  expect(state.activeDispatches[1].location).toBeNull();
  expect(state.activeDispatches[1].destination).toBeNull();
  expect(state.availableNurses.map(n => n.id)).toEqual(['n1']);
  expect(state.waitingCases[0].id).toBe('waiting');
});

test('failed refresh exposes the error and preserves previously loaded data', async () => {
  useDispatchStore.setState({ activeDispatches: [{ id: 'saved-case' }] });
  api.get.mockRejectedValue(new Error('Session expired'));
  await useDispatchStore.getState().fetchDispatchData();
  expect(useDispatchStore.getState().error).toBe('Session expired');
  expect(useDispatchStore.getState().activeDispatches[0].id).toBe('saved-case');
  expect(useDispatchStore.getState().isLoading).toBe(false);
});
