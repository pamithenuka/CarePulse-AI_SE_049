import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import RegisterPatientPage from './RegisterPatientPage';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/patientsApi');

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/patients/new']}>
      <Routes>
        <Route path="/patients/new" element={<RegisterPatientPage />} />
        <Route path="/patients/:id" element={<div>Patient Detail Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

const fillRequiredFields = () => {
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'new.patient@carepulse.dev' } });
  fireEvent.change(screen.getByLabelText(/temporary password/i), { target: { value: 'Patient@12345' } });
  fireEvent.change(screen.getByLabelText(/full name/i), { target: { value: 'Test Patient' } });
  fireEvent.change(screen.getByLabelText(/date of birth/i), { target: { value: '1990-01-01' } });
  fireEvent.change(screen.getByLabelText(/phone number/i), { target: { value: '0771234567' } });
  fireEvent.change(screen.getByLabelText(/national id/i), { target: { value: '199012345V' } });
};

describe('RegisterPatientPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('rejects submission with an invalid email and a short password', async () => {
    renderPage();

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'not-an-email' } });
    fireEvent.change(screen.getByLabelText(/temporary password/i), { target: { value: 'short' } });
    fireEvent.click(screen.getByRole('button', { name: /register patient/i }));

    expect(await screen.findByText(/enter a valid email address/i)).toBeInTheDocument();
    expect(screen.getByText(/password must be at least 8 characters/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test('registers a patient and navigates to their detail page on success', async () => {
    patientsApi.registerPatient.mockResolvedValueOnce({ id: 'new-patient-id' });

    renderPage();
    fillRequiredFields();
    fireEvent.click(screen.getByRole('button', { name: /register patient/i }));

    await waitFor(() => expect(patientsApi.registerPatient).toHaveBeenCalled());
    expect(await screen.findByText('Patient Detail Page')).toBeInTheDocument();
  });

  test('shows a server error message when registration fails', async () => {
    patientsApi.registerPatient.mockRejectedValueOnce({ response: { data: { message: 'An account with this email already exists.' } } });

    renderPage();
    fillRequiredFields();
    fireEvent.click(screen.getByRole('button', { name: /register patient/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with this email already exists.');
  });
});
