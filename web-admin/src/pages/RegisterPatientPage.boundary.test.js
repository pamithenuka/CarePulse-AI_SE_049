import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import RegisterPatientPage from './RegisterPatientPage';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/patientsApi');

// Student 1 (QM): boundary / invalid / failure tests for the patient registration form.

const VALID = {
  email: 'new.patient@carepulse.dev',
  password: 'Patient@12345',
  fullName: 'Test Patient',
  dob: '1990-01-01',
  phone: '0771234567',
  nic: '199012345V',
};

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

function fill(overrides = {}) {
  const v = { ...VALID, ...overrides };
  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: v.email } });
  fireEvent.change(screen.getByLabelText(/temporary password/i), { target: { value: v.password } });
  fireEvent.change(screen.getByLabelText(/full name/i), { target: { value: v.fullName } });
  fireEvent.change(screen.getByLabelText(/date of birth/i), { target: { value: v.dob } });
  fireEvent.change(screen.getByLabelText(/phone number/i), { target: { value: v.phone } });
  fireEvent.change(screen.getByLabelText(/national id/i), { target: { value: v.nic } });
}

const submit = () => fireEvent.click(screen.getByRole('button', { name: /register patient/i }));
const isoDay = (offsetDays) => new Date(Date.now() + offsetDays * 86400000).toISOString().slice(0, 10);

describe('RegisterPatientPage boundary and invalid input', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    patientsApi.registerPatient.mockResolvedValue({ id: 'new-id' });
  });

  test.each([
    ['7-character password is rejected', 'abcdefg', false],
    ['8-character password is accepted', 'abcdefgh', true],
  ])('%s', async (_name, password, accepted) => {
    renderPage();
    fill({ password });
    submit();

    if (accepted) {
      await waitFor(() => expect(patientsApi.registerPatient).toHaveBeenCalledTimes(1));
    } else {
      expect(await screen.findByText(/password must be at least 8 characters/i)).toBeInTheDocument();
      expect(patientsApi.registerPatient).not.toHaveBeenCalled();
    }
  });

  test('a date of birth of today is accepted', async () => {
    renderPage();
    fill({ dob: isoDay(0) });
    submit();

    await waitFor(() => expect(patientsApi.registerPatient).toHaveBeenCalledTimes(1));
  });

  test('a date of birth of tomorrow is rejected', async () => {
    renderPage();
    fill({ dob: isoDay(1) });
    submit();

    expect(await screen.findByText(/cannot be in the future/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test.each([
    ['11 digits', '19901234567'],
    ['13 digits', '1990123456789'],
    ['12 digits with a letter', '19901234567V'],
    ['9 digits without V/X', '199012345'],
    ['10 digits + V', '1990123456V'],
    ['empty', ''],
  ])('invalid national ID (%s) is rejected', async (_name, nic) => {
    renderPage();
    fill({ nic });
    submit();

    expect(await screen.findByText(/9 digits \+ V\/X, or 12 digits/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test.each([
    ['9 digits + lowercase x', '199012345x'],
    ['12 digits', '199012345678'],
  ])('valid national ID (%s) is accepted', async (_name, nic) => {
    renderPage();
    fill({ nic });
    submit();

    await waitFor(() => expect(patientsApi.registerPatient).toHaveBeenCalledTimes(1));
  });

  test.each([
    ['9 digits', '077123456'],
    ['11 digits', '07712345678'],
    ['no leading 0', '7712345678'],
    ['letters', '07712abcde'],
  ])('invalid phone number (%s) is rejected', async (_name, phone) => {
    renderPage();
    fill({ phone });
    submit();

    expect(await screen.findByText(/10-digit number starting with 0/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test('a partly filled emergency contact row blocks submission', async () => {
    renderPage();
    fill();
    fireEvent.change(screen.getByPlaceholderText('Full name'), { target: { value: 'Only A Name' } });
    submit();

    expect(await screen.findByText(/fill in name, relationship and a valid phone number/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test('all required fields empty shows an error for each and sends nothing', async () => {
    renderPage();
    submit();

    expect(await screen.findByText(/enter a valid email address/i)).toBeInTheDocument();
    expect(screen.getByText(/full name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/date of birth is required/i)).toBeInTheDocument();
    expect(patientsApi.registerPatient).not.toHaveBeenCalled();
  });

  test('the submit button is disabled while the request is in flight (no double submit)', async () => {
    let resolveRequest;
    patientsApi.registerPatient.mockReturnValue(new Promise((resolve) => { resolveRequest = resolve; }));
    renderPage();
    fill();
    const button = screen.getByRole('button', { name: /register patient/i });
    fireEvent.click(button);

    await waitFor(() => expect(button).toBeDisabled());
    fireEvent.click(button);
    expect(patientsApi.registerPatient).toHaveBeenCalledTimes(1);
    resolveRequest({ id: 'new-id' });
  });

  test('network failure without a response body still shows an error and stays on the form', async () => {
    patientsApi.registerPatient.mockRejectedValueOnce(new Error('Network Error'));
    renderPage();
    fill();
    submit();

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.queryByText('Patient Detail Page')).not.toBeInTheDocument();
  });

  // Mobile (validators.dart) trims input before validating; the web form should behave the same,
  // otherwise a value copied with a trailing space is accepted on the phone but rejected on the web.
  test('leading/trailing spaces around email and national ID are tolerated, as on mobile', async () => {
    renderPage();
    fill({ email: '  new.patient@carepulse.dev  ', nic: ' 199012345V ' });
    submit();

    await waitFor(() => expect(patientsApi.registerPatient).toHaveBeenCalledTimes(1));
    const payload = patientsApi.registerPatient.mock.calls[0][0];
    expect(payload.email).toBe('new.patient@carepulse.dev');
    expect(payload.nationalId).toBe('199012345V');
  });
});
