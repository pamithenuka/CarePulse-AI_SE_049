import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import LoginPage from './LoginPage';
import { AuthProvider } from '../context/AuthContext';
import * as authApi from '../api/authApi';

jest.mock('../api/authApi');

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/patients" element={<div>Patients Page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    jest.clearAllMocks();
  });

  test('requires email and password before submitting', () => {
    renderLoginPage();
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/password/i);

    expect(emailInput).toBeRequired();
    expect(passwordInput).toBeRequired();
  });

  test('shows an error message when login fails', async () => {
    authApi.login.mockRejectedValueOnce({ response: { data: { message: 'Invalid email or password.' } } });

    renderLoginPage();
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrong-password' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
  });

  test('navigates to patients after a successful login', async () => {
    authApi.login.mockResolvedValueOnce({
      userId: '1',
      fullName: 'Dr. Amara Silva',
      email: 'doctor@carepulse.dev',
      roles: ['Doctor'],
      token: 'test-token',
      expiresAt: new Date().toISOString(),
    });

    renderLoginPage();
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Doctor@12345' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => expect(screen.getByText('Patients Page')).toBeInTheDocument());
  });
});
