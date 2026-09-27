import { createContext, useContext, useEffect, useState } from 'react';
import { api } from '../api/client';

const DoctorContext = createContext(null);

export function DoctorProvider({ children }) {
  const [doctors, setDoctors] = useState([]);
  const [selectedDoctorId, setSelectedDoctorId] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');

  function refreshDoctors() {
    setLoading(true);
    api
      .getDoctors()
      .then((list) => {
        setDoctors(list);
        if (!selectedDoctorId && list.length > 0) setSelectedDoctorId(list[0].id);
      })
      .catch((err) => setLoadError(err.message))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    refreshDoctors();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedDoctor = doctors.find((d) => d.id === selectedDoctorId);

  return (
    <DoctorContext.Provider
      value={{
        doctors,
        selectedDoctorId,
        setSelectedDoctorId,
        selectedDoctor,
        loading,
        loadError,
        refreshDoctors
      }}
    >
      {children}
    </DoctorContext.Provider>
  );
}

export function useDoctor() {
  const ctx = useContext(DoctorContext);
  if (!ctx) throw new Error('useDoctor must be used within DoctorProvider');
  return ctx;
}
