import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProtectedRoute from './ProtectedRoute';
import { AuthProvider } from '../context/AuthContext';

function renderWithAuth(initialEntry, allowedRoles) {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route path="/unauthorized" element={<div>Unauthorized Page</div>} />
          <Route element={<ProtectedRoute allowedRoles={allowedRoles} />}>
            <Route path="/patients" element={<div>Patients Page</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  test('redirects to login when no user is signed in', () => {
    renderWithAuth('/patients', ['Doctor']);
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  test('redirects to unauthorized when the signed-in user lacks the required role', () => {
    localStorage.setItem('carepulse_token', 'test-token');
    localStorage.setItem(
      'carepulse_user',
      JSON.stringify({ userId: '1', fullName: 'Nina Nurse', email: 'nina@carepulse.dev', roles: ['Nurse'] })
    );

    renderWithAuth('/patients', ['Doctor', 'Admin']);
    expect(screen.getByText('Unauthorized Page')).toBeInTheDocument();
  });

  test('renders the protected content when the user has an allowed role', () => {
    localStorage.setItem('carepulse_token', 'test-token');
    localStorage.setItem(
      'carepulse_user',
      JSON.stringify({ userId: '1', fullName: 'Dr. Amara Silva', email: 'doctor@carepulse.dev', roles: ['Doctor'] })
    );

    renderWithAuth('/patients', ['Doctor', 'Admin']);
    expect(screen.getByText('Patients Page')).toBeInTheDocument();
  });
});
