export function point(lat, lng) {
  if (lat == null || lng == null || lat === '' || lng === '') return null;
  const latitude = Number(lat), longitude = Number(lng);
  return Number.isFinite(latitude) && Number.isFinite(longitude) &&
    Math.abs(latitude) <= 90 && Math.abs(longitude) <= 180
    ? { lat: latitude, lng: longitude } : null;
}

export function mapCases(active, waiting, nurses, previews, now = Date.now()) {
  const location = (n) => {
    const age = now - Date.parse(n.locationRecordedAt);
    return { location: n.locationRecordedAt ? point(n.currentLat, n.currentLng) : null,
      stale: !Number.isFinite(age) || age < 0 || age > 5 * 60 * 1000 };
  };
  return [
    ...active.map(d => ({ ...d, ...location(d), key: `active-${d.id}`,
      destination: point(d.destinationLat, d.destinationLng), label: d.patientName || 'Patient',
      nurseSelected: true })),
    ...waiting.map(t => {
      const preview = previews[t.id];
      const nurse = nurses.find(n => n.id === preview?.nurseId);
      return { key: `waiting-${t.id}`, label: t.patientName || 'Patient', status: 'Waiting for assignment',
        destination: point(preview ? preview.lat : t.latitude, preview ? preview.lng : t.longitude),
        nurseName: nurse?.name, nurseSelected: Boolean(preview?.nurseId),
        ...(nurse ? location(nurse) : { location: null, stale: true }),
        locationRecordedAt: nurse?.locationRecordedAt };
    })
  ];
}
