import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import PatientDetailPage from './PatientDetailPage';
import { AuthProvider } from '../context/AuthContext';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/patientsApi');

const baseProfile = {
  id: 'p1',
  fullName: 'Jane Perera',
  dateOfBirth: '1990-05-01',
  age: 36,
  gender: 'Female',
  bloodType: 'O+',
  phoneNumber: '0771234567',
  address: '1 Main St',
  nationalId: '199012345V',
  allergies: 'Penicillin',
  status: 'Active',
  lastEmergencyBroadcastAt: null,
  createdAt: new Date().toISOString(),
  emergencyContacts: [{ id: 'c1', fullName: 'Sam Perera', relationshipToPatient: 'Spouse', phoneNumber: '0779876543', isPrimary: true }],
  medicalHistories: [
    { id: 'h1', conditionName: 'Asthma', notes: null, diagnosedOn: '2020-01-01', isChronic: true, currentMedications: 'Inhaler', isResolved: false, resolvedOn: null, createdAt: new Date().toISOString() },
  ],
  medicalDocuments: [],
};

function renderAsRole(roles) {
  localStorage.setItem('carepulse_token', 'test-token');
  localStorage.setItem('carepulse_user', JSON.stringify({ userId: 'u1', fullName: 'Dr. Test', email: 'doc@carepulse.dev', roles }));

  return render(
    <MemoryRouter initialEntries={['/patients/p1']}>
      <AuthProvider>
        <Routes>
          <Route path="/patients/:id" element={<PatientDetailPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('PatientDetailPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test('shows an allergy warning badge and switches tabs', async () => {
    patientsApi.getPatientProfile.mockResolvedValueOnce(baseProfile);

    renderAsRole(['Doctor']);

    expect(await screen.findByText(/⚠ Allergy/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: /medical history/i }));
    expect(await screen.findByText('Asthma')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: /emergency contacts/i }));
    expect(await screen.findByText('Sam Perera')).toBeInTheDocument();
  });

  test('hides the Edit button on Overview for a Doctor (Admin only)', async () => {
    patientsApi.getPatientProfile.mockResolvedValueOnce(baseProfile);

    renderAsRole(['Doctor']);

    await screen.findByText('Jane Perera');
    expect(screen.queryByRole('button', { name: /^edit$/i })).not.toBeInTheDocument();
  });

  test('shows an error state when the profile fails to load', async () => {
    patientsApi.getPatientProfile.mockRejectedValueOnce({ response: { data: { message: 'Patient profile not found.' } } });

    renderAsRole(['Doctor']);

    expect(await screen.findByRole('alert')).toHaveTextContent('Patient profile not found.');
  });

  test('Admin can trigger an emergency alert and sees the result', async () => {
    patientsApi.getPatientProfile.mockResolvedValue(baseProfile);
    patientsApi.triggerEmergencyAlert.mockResolvedValueOnce({
      alertLogId: 'a1',
      triggeredAt: new Date().toISOString(),
      notifiedContacts: [{ contactName: 'Sam Perera', phoneNumber: '0779876543', deliveryStatus: 0 }],
    });
    window.confirm = jest.fn(() => true);

    renderAsRole(['Admin']);

    await screen.findByText('Jane Perera');
    fireEvent.click(screen.getByRole('button', { name: /trigger emergency alert/i }));

    await waitFor(() => expect(screen.getByText(/emergency alert sent to 1 contact/i)).toBeInTheDocument());
  });
});
