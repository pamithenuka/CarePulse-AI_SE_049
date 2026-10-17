import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import SlotsPage from './SlotsPage';
import { api } from '../api/client';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/client', () => ({
  api: { getSlots: jest.fn(), bookAppointment: jest.fn() },
}));
jest.mock('../api/patientsApi');
jest.mock('../components/DoctorPicker', () => () => null);
jest.mock('../components/DoctorContext', () => ({
  useDoctor: () => ({ selectedDoctor: { fullName: 'Dr. Test' }, selectedDoctorId: 'doc-1' }),
}));

const slot = { slotId: 's1', doctorId: 'doc-1', doctorName: 'Dr. Test', specialty: 'CARDIOLOGY', slotStart: '2026-10-20T03:00:00Z', slotEnd: '2026-10-20T03:30:00Z' };
const nimal = { id: 'patient-1', fullName: 'Nimal Perera', nationalId: '199012345V', age: 35, gender: 'Male' };

async function chooseNimal() {
  fireEvent.focus(screen.getByRole('combobox'));
  fireEvent.mouseDown(await screen.findByRole('option', { name: /nimal perera/i }));
}

describe('SlotsPage patient selection', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    api.getSlots.mockResolvedValue([slot]);
    patientsApi.getPatients.mockResolvedValue({ items: [nimal] });
  });

  test('no patient ID box any more; booking is disabled until a patient is chosen', async () => {
    render(<SlotsPage />);
    const book = await screen.findByRole('button', { name: /book appointment/i });

    expect(screen.queryByPlaceholderText(/patient profile id/i)).not.toBeInTheDocument();
    expect(book).toBeDisabled();
    expect(screen.getByText(/choose a patient above to enable booking/i)).toBeInTheDocument();

    await chooseNimal();

    expect(book).toBeEnabled();
    expect(screen.queryByText(/choose a patient above/i)).not.toBeInTheDocument();
  });

  test('booking sends the chosen patient\'s id and names the patient in the confirmation', async () => {
    api.bookAppointment.mockResolvedValueOnce({});
    render(<SlotsPage />);
    await chooseNimal();

    fireEvent.click(await screen.findByRole('button', { name: /book appointment/i }));

    expect(await screen.findByText('Appointment booked for Nimal Perera.')).toBeInTheDocument();
    expect(api.bookAppointment).toHaveBeenCalledWith({ slotId: 's1', patientId: 'patient-1' });
    await waitFor(() => expect(api.getSlots).toHaveBeenCalledTimes(2));
  });

  test('a booking conflict is shown as an error and no success message appears', async () => {
    api.bookAppointment.mockRejectedValueOnce(new Error('This slot is no longer available.'));
    render(<SlotsPage />);
    await chooseNimal();

    fireEvent.click(await screen.findByRole('button', { name: /book appointment/i }));

    expect(await screen.findByText('This slot is no longer available.')).toBeInTheDocument();
    expect(screen.queryByText(/appointment booked/i)).not.toBeInTheDocument();
  });

  test('changing the patient disables booking again', async () => {
    render(<SlotsPage />);
    const book = await screen.findByRole('button', { name: /book appointment/i });
    await chooseNimal();
    expect(book).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: /change/i }));

    expect(book).toBeDisabled();
  });
});
