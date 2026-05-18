import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import RegistrationPage from './RegistrationPage';
import styles from './RegistrationPage.module.css';

vi.mock('../components/auth/RegistrationForm', () => ({
  default: () => <div>Registration form placeholder</div>,
}));

describe('RegistrationPage', () => {
  it('renders the auth brand shell and wireframe subtitle', () => {
    render(
      <BrowserRouter>
        <RegistrationPage />
      </BrowserRouter>
    );

    expect(screen.getByLabelText(/unified patient access platform/i)).toBeInTheDocument();
    expect(screen.getByText('Propel Patient Portal')).toBeInTheDocument();
    expect(screen.getByText('Unified Patient Access & Clinical Intelligence')).toBeInTheDocument();
    expect(screen.getByText('Register to book appointments and manage your health.')).toBeInTheDocument();
  });

  it('applies page and card shell classes for wireframe layout contract', () => {
    const { container } = render(
      <BrowserRouter>
        <RegistrationPage />
      </BrowserRouter>
    );

    const page = container.querySelector('main');
    const card = container.querySelector('section');

    expect(page).toHaveClass(styles.page);
    expect(card).toHaveClass(styles.card);
  });
});
