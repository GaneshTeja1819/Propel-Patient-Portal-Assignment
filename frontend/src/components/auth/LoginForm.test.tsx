import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import LoginForm from './LoginForm';
import styles from './LoginForm.module.css';

const loginWithRedirectMock = vi.fn();

vi.mock('../../hooks/useLogin', () => ({
  useLogin: () => ({
    loginWithRedirect: loginWithRedirectMock,
    cookiesRequired: false,
  }),
}));

describe('LoginForm', () => {
  it('renders labeled email and password fields', () => {
    render(
      <BrowserRouter>
        <LoginForm />
      </BrowserRouter>
    );

    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i, { selector: 'input' })).toBeInTheDocument();
    expect(screen.getByPlaceholderText('e.g. sarah.johnson@email.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your password')).toBeInTheDocument();
    expect(screen.getByLabelText(/keep me signed in for 30 days/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /create an account/i })).toHaveClass(styles.secondaryActionButton);
    expect(screen.getByRole('link', { name: /reset your password/i })).toHaveClass(styles.inlineLink);
    expect(screen.getByText(/don't have an account\?/i)).toBeInTheDocument();
    const createLink = screen.getByRole('link', { name: /create one here/i });
    expect(createLink).toHaveClass(styles.inlineLink);
    expect(createLink.closest('p')).not.toBeNull();
    expect(screen.getByText(/don't have an account\?/i).closest('div')).toHaveClass(styles.authFooter);
    expect(screen.getByText('Patient (Sarah J.)')).toBeInTheDocument();
    expect(screen.getByText('Staff (Alex T.)')).toBeInTheDocument();
    expect(screen.getByText('Admin (Jennifer P.)')).toBeInTheDocument();
  });

  it('toggles password visibility', async () => {
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <LoginForm />
      </BrowserRouter>
    );

    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' });
    const toggleButton = screen.getByRole('button', { name: /show password/i });

    expect(passwordInput).toHaveAttribute('type', 'password');
    expect(toggleButton).toHaveTextContent('👁');

    await user.click(toggleButton);

    expect(passwordInput).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: /hide password/i })).toHaveTextContent('🙈');
  });

  it('shows exact wireframe invalid credentials message on failed login', async () => {
    const user = userEvent.setup();

    loginWithRedirectMock.mockResolvedValueOnce({
      ok: false,
      message: 'Invalid email or password',
    });

    render(
      <BrowserRouter>
        <LoginForm />
      </BrowserRouter>
    );

    await user.type(screen.getByLabelText(/email address/i), 'User@Example.com');
    await user.type(screen.getByLabelText(/password/i, { selector: 'input' }), 'wrong-password');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(
      screen.getByText('⚠ Your email or password is incorrect. Please try again.')
    ).toBeInTheDocument();
  });

  it('renders field-level error icon before error text', async () => {
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <LoginForm />
      </BrowserRouter>
    );

    await user.click(screen.getByRole('button', { name: /sign in/i }));

    const emailErrorMessage = screen.getByText('Email address is required.');
    const emailErrorRow = emailErrorMessage.closest('p');

    expect(emailErrorRow).not.toBeNull();

    const emailErrorIcon = emailErrorRow?.firstElementChild as HTMLElement | null;

    expect(emailErrorIcon).not.toBeNull();
    if (!emailErrorIcon) {
      throw new Error('Expected email error icon to be rendered');
    }
    expect(emailErrorIcon).toHaveClass(styles.fieldErrorIcon);
    expect(emailErrorIcon).toHaveClass(styles.fieldErrorIconTriangle);
    expect(emailErrorIcon).toHaveTextContent('!');
    expect(emailErrorIcon.compareDocumentPosition(emailErrorMessage) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('does not render label-level warning indicators for required login fields', () => {
    render(
      <BrowserRouter>
        <LoginForm />
      </BrowserRouter>
    );

    expect(screen.queryByTestId('required-indicator')).not.toBeInTheDocument();
  });
});
