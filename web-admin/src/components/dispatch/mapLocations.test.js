import { mapCases, point } from './mapLocations';
const now = Date.parse('2026-10-15T10:00:00Z');
test('waiting destination and selected nurse appear before assignment with edited coordinates', () => {
  const result = mapCases([], [{ id: 'p', latitude: 6, longitude: 79 }],
    [{ id: 'n', name: 'Nurse', currentLat: 7, currentLng: 80, locationRecordedAt: '2026-10-15T09:59:00Z' }],
    { p: { nurseId: 'n', lat: '6.5', lng: '79.5' } }, now)[0];
  expect(result.destination).toEqual({ lat: 6.5, lng: 79.5 });
  expect(result.location).toEqual({ lat: 7, lng: 80 });
  expect(result.stale).toBe(false);
});
test('active dispatch preserves overlapping positions and marks old GPS stale', () => {
  const result = mapCases([{ id: 'd', currentLat: 6, currentLng: 79, destinationLat: 6, destinationLng: 79,
    locationRecordedAt: '2026-10-15T09:50:00Z' }], [], [], {}, now)[0];
  expect(result.location).toEqual(result.destination);
  expect(result.stale).toBe(true);
});
test('missing GPS does not invent a position and valid zero coordinates remain visible', () => {
  const result = mapCases([], [{ id: 'p', latitude: 0, longitude: 0 }], [], {}, now)[0];
  expect(result.destination).toEqual({ lat: 0, lng: 0 });
  expect(result.location).toBeNull();
  expect(point('', 79)).toBeNull();
  expect(point(91, 79)).toBeNull();
  expect(point(6, NaN)).toBeNull();
});
