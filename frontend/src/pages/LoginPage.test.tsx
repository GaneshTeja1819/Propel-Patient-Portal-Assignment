import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import LoginPage from './LoginPage';
import styles from './LoginPage.module.css';

vi.mock('../components/auth/LoginForm', () => ({
  default: () => <div>Login form placeholder</div>,
}));

describe('LoginPage', () => {
  it('shows expired session banner when reason is expired', () => {
    render(
      <MemoryRouter initialEntries={['/login?reason=expired']}>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Session expired. Please log in again.')).toBeInTheDocument();
  });

  it('does not show expired session banner without reason', () => {
    render(
      <MemoryRouter initialEntries={['/login']}>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.queryByText('Session expired. Please log in again.')).not.toBeInTheDocument();
  });

  it('shows registration success banner when state is present', () => {
    render(
      <MemoryRouter initialEntries={[{ pathname: '/login', state: { registrationSuccess: true } }]}>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Account created successfully. Please sign in.')).toBeInTheDocument();
  });

  it('renders the auth brand shell', () => {
    render(
      <MemoryRouter initialEntries={['/login']}>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByLabelText(/unified patient access platform/i)).toBeInTheDocument();
    expect(screen.getByText('Propel Patient Portal')).toBeInTheDocument();
    expect(screen.getByText('Unified Patient Access & Clinical Intelligence')).toBeInTheDocument();
  });

  it('applies page and card shell classes for wireframe layout contract', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/login']}>
        <LoginPage />
      </MemoryRouter>
    );

    const page = container.querySelector('main');
    const card = container.querySelector('section');

    expect(page).toHaveClass(styles.page);
    expect(card).toHaveClass(styles.card);
  });
});
