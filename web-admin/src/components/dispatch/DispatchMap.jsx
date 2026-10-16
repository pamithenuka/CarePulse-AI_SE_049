import React, { useEffect } from 'react';
import { MapContainer, TileLayer, CircleMarker, Popup, Tooltip, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import { useDispatchStore } from '../../store/useDispatchStore';
import { mapCases } from './mapLocations';
import './DispatchMap.css';
function FitCases({ geometry }) {
  const map = useMap();
  useEffect(() => {
    const points = JSON.parse(geometry);
    if (points.length) map.fitBounds(points, { padding: [30, 30], maxZoom: 14 });
  }, [geometry, map]);
  return null;
}
export const DispatchMap = () => {
  const { activeDispatches, waitingCases, availableNurses, mapPreviews, fetchDispatchData } = useDispatchStore();
  useEffect(() => { fetchDispatchData(); const timer = setInterval(fetchDispatchData, 10000); return () => clearInterval(timer); }, [fetchDispatchData]);
  const cases = mapCases(activeDispatches, waitingCases, availableNurses, mapPreviews);
  const locationWarnings = cases.filter(d => d.nurseSelected && (!d.location || d.stale));
  const geometry = JSON.stringify(cases.flatMap(d => [d.location, d.destination].filter(Boolean).map(p => [p.lat, p.lng])));
  return <div className="dispatch-map-container">
    {locationWarnings.length > 0 && <div className="dispatch-map-legend" aria-label="Location warnings">
      {locationWarnings.map(d =>
        <div key={d.key} role="status">{d.nurseName || d.label}: {d.location ? 'Nurse location is stale — ETA unavailable.' : 'Nurse location unavailable — ETA unavailable.'}</div>)}
    </div>}
    <MapContainer center={[6.9271, 79.8612]} zoom={11} className="leaflet-map-wrapper">
      <TileLayer attribution='&copy; OpenStreetMap contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      <FitCases geometry={geometry} />
      {cases.map(d => <React.Fragment key={d.key}>
        {d.location && <CircleMarker center={[d.location.lat, d.location.lng]} radius={16}
          pathOptions={{ color: 'blue', fillOpacity: 0, weight: 4, dashArray: d.stale ? '5 4' : undefined }}>
          <Tooltip permanent direction="bottom" offset={[0, 18]} opacity={1} className="dispatch-person-label dispatch-nurse-label">
            <span className="dispatch-person-name">{d.nurseName || 'Name unavailable'}</span>
          </Tooltip>
          <Popup>{d.nurseName} • {d.status}<br />Last GPS update: {new Date(d.locationRecordedAt).toLocaleString()}
            {d.stale && <><br />Stale location; not used for ETA.</>}</Popup>
        </CircleMarker>}
        {d.destination && <CircleMarker center={[d.destination.lat, d.destination.lng]} radius={7}
          pathOptions={{ color: 'red', fillOpacity: 0.6 }}>
          <Tooltip permanent direction="top" offset={[0, -10]} opacity={1} className="dispatch-person-label dispatch-patient-label">
            <span className="dispatch-person-name">{d.label}</span>
          </Tooltip>
          <Popup>{d.label} • {d.status}<br />Patient destination{d.status === 'Waiting for assignment' ? ' preview — confirm before assigning' : ''}</Popup>
        </CircleMarker>}
      </React.Fragment>)}
    </MapContainer>
  </div>;
};
