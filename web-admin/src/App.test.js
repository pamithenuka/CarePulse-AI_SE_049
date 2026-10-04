import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import App from './App';

test('unauthenticated users are redirected to the login page', () => {
  render(
    <MemoryRouter initialEntries={['/']}>
      <App />
    </MemoryRouter>
  );

  expect(screen.getByText(/CarePulse Clinical Portal/i)).toBeInTheDocument();
});

test('unknown routes render the not found page when authenticated bypass is absent', () => {
  render(
    <MemoryRouter initialEntries={['/does-not-exist']}>
      <App />
    </MemoryRouter>
  );

  expect(screen.getByText(/Page not found/i)).toBeInTheDocument();
});
