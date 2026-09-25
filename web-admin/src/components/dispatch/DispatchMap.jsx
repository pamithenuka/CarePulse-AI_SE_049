import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import { useDispatchStore } from '../../store/useDispatchStore';
import L from 'leaflet';
import './DispatchMap.css';

// Fix Leaflet's default icon path issues
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: require('leaflet/dist/images/marker-icon-2x.png'),
  iconUrl: require('leaflet/dist/images/marker-icon.png'),
  shadowUrl: require('leaflet/dist/images/marker-shadow.png'),
});

const customNurseIcon = new L.Icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/2869/2869812.png',
  iconSize: [32, 32],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32]
});

const customPatientIcon = new L.Icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/3004/3004458.png',
  iconSize: [32, 32],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32]
});

export const DispatchMap = () => {
  const { activeDispatches, fetchDispatchData } = useDispatchStore();

  useEffect(() => {
    // Initial fetch
    fetchDispatchData();
    
    // Set up polling every 10 seconds for live GPS updates
    const interval = setInterval(() => {
      fetchDispatchData();
    }, 10000);

    return () => clearInterval(interval);
  }, [fetchDispatchData]);

  // Center on NYC as default
  const center = [40.7300, -73.9900];

  return (
    <div className="dispatch-map-container">
      <MapContainer center={center} zoom={13} className="leaflet-map-wrapper">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        
        {activeDispatches.map((dispatch) => (
          <React.Fragment key={dispatch.id}>
            {/* Nurse Marker */}
            <Marker position={[dispatch.location.lat, dispatch.location.lng]} icon={customNurseIcon}>
              <Popup>
                <div className="custom-popup">
                  <strong>{dispatch.nurseName}</strong>
                  <div className={`status-badge ${dispatch.status.toLowerCase()}`}>
                    {dispatch.status}
                  </div>
                </div>
              </Popup>
            </Marker>
            
            {/* Patient/Destination Marker */}
            <Marker position={[dispatch.destination.lat, dispatch.destination.lng]} icon={customPatientIcon}>
              <Popup>
                <div className="custom-popup">
                  <strong>Emergency Destination</strong>
                  <p>Dispatch ID: {dispatch.id}</p>
                </div>
              </Popup>
            </Marker>

            {/* Route Trail */}
            <Polyline 
              positions={[
                [dispatch.location.lat, dispatch.location.lng], 
                [dispatch.destination.lat, dispatch.destination.lng]
              ]} 
              color={dispatch.status === 'EnRoute' ? '#3b82f6' : '#22c55e'}
              dashArray={dispatch.status === 'EnRoute' ? '5, 10' : ''}
              weight={4}
              opacity={0.7}
            />
          </React.Fragment>
        ))}
      </MapContainer>
    </div>
  );
};
