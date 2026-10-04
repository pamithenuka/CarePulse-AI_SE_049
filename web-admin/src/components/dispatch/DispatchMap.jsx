import React, { useEffect } from 'react';
import { MapContainer, TileLayer, CircleMarker, Popup, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import { useDispatchStore } from '../../store/useDispatchStore';
import './DispatchMap.css';
function FitCases({ dispatches }) {
  const map = useMap();
  useEffect(() => {
    const points = dispatches.flatMap(d => [d.location, d.destination].filter(Boolean).map(p => [p.lat, p.lng]));
    if (points.length) map.fitBounds(points, { padding: [30, 30], maxZoom: 14 });
  }, [dispatches, map]);
  return null;
}
export const DispatchMap = () => {
  const { activeDispatches, fetchDispatchData } = useDispatchStore();
  useEffect(() => { fetchDispatchData(); const timer = setInterval(fetchDispatchData, 10000); return () => clearInterval(timer); }, [fetchDispatchData]);
  return <div className="dispatch-map-container">
    <MapContainer center={[6.9271, 79.8612]} zoom={11} className="leaflet-map-wrapper">
      <TileLayer attribution='&copy; OpenStreetMap contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
      <FitCases dispatches={activeDispatches} />
      {activeDispatches.map(d => <React.Fragment key={d.id}>
        {d.location && <CircleMarker center={[d.location.lat, d.location.lng]} radius={9} pathOptions={{color: 'blue'}}><Popup>{d.nurseName} • {d.status}<br />Last update: {new Date(d.locationRecordedAt).toLocaleString()}</Popup></CircleMarker>}
        {d.destination && <CircleMarker center={[d.destination.lat, d.destination.lng]} radius={9} pathOptions={{color: 'red'}}><Popup>Confirmed patient destination</Popup></CircleMarker>}
      </React.Fragment>)}
    </MapContainer>
  </div>;
};
