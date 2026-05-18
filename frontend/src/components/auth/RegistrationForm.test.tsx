import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import RegistrationForm from './RegistrationForm';
import styles from './RegistrationForm.module.css';

const registerUserMock = vi.fn(async () => true);
const navigateMock = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => navigateMock,
  };
});

vi.mock('../../hooks/useRegistration', () => ({
  useRegistration: () => ({
    isSubmitting: false,
    submissionError: null,
    registerUser: registerUserMock,
  }),
}));

describe('RegistrationForm', () => {
  beforeEach(() => {
    navigateMock.mockReset();
    registerUserMock.mockClear();
  });

  it('renders all parity fields and footer content', () => {
    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/date of birth/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/phone number/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Create a strong password')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Re-enter your password')).toBeInTheDocument();
    expect(screen.getByText(/terms of service/i)).toBeInTheDocument();
    expect(screen.getByText(/your health data is protected under hipaa/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /sign in instead/i })).toHaveClass(styles.secondaryActionLink);
    const strengthTrack = screen.getByTestId('password-strength-track');
    const passwordHint = screen.getByText('Minimum 8 characters, at least one uppercase letter and one number.');

    expect(strengthTrack).toBeInTheDocument();
    expect(passwordHint).toBeInTheDocument();
    expect(strengthTrack.compareDocumentPosition(passwordHint) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('renders all fields with associated labels', () => {
    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i, { selector: 'input#password' })).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i, { selector: 'input#confirmPassword' })).toBeInTheDocument();
  });

  it('shows failing password rule inline and keeps focus on password on submit', async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    await user.type(screen.getByLabelText(/first name/i), 'Sarah');
    await user.type(screen.getByLabelText(/last name/i), 'Johnson');
    await user.type(screen.getByLabelText(/email address/i), 'sarah@example.com');
    await user.type(screen.getByLabelText(/date of birth/i), '1990-01-01');
    await user.type(screen.getByLabelText(/password/i, { selector: 'input#password' }), 'short');
    await user.type(screen.getByLabelText(/confirm password/i, { selector: 'input#confirmPassword' }), 'short');

    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(screen.getByText('Minimum 8 characters, at least one uppercase letter and one number.')).toBeInTheDocument();
    expect(screen.getByText('Password does not meet requirements.')).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i, { selector: 'input#password' })).toHaveFocus();
    expect(registerUserMock).not.toHaveBeenCalled();
  });

  it('shows success cue and redirects to login on successful registration', async () => {
    const user = userEvent.setup();

    registerUserMock.mockResolvedValueOnce(true);

    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    await user.type(screen.getByLabelText(/first name/i), 'Sarah');
    await user.type(screen.getByLabelText(/last name/i), 'Johnson');
    await user.type(screen.getByLabelText(/email address/i), 'sarah@example.com');
    await user.type(screen.getByLabelText(/date of birth/i), '1990-01-01');
    await user.type(screen.getByLabelText(/password/i, { selector: 'input#password' }), 'StrongPass1');
    await user.type(screen.getByLabelText(/confirm password/i, { selector: 'input#confirmPassword' }), 'StrongPass1');

    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(
      await screen.findByText('Account created successfully. Redirecting to login...')
    ).toBeInTheDocument();
  });

  it('toggles password visibility while keeping helper content rendered', async () => {
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    await user.type(screen.getByLabelText(/first name/i), 'Sarah');
    await user.type(screen.getByLabelText(/last name/i), 'Johnson');
    await user.type(screen.getByLabelText(/email address/i), 'sarah@example.com');
    await user.type(screen.getByLabelText(/date of birth/i), '1990-01-01');
    await user.type(screen.getByLabelText(/password/i, { selector: 'input#password' }), 'short');
    await user.type(screen.getByLabelText(/confirm password/i, { selector: 'input#confirmPassword' }), 'short');
    await user.click(screen.getByRole('button', { name: /create account/i }));

    const passwordErrorRow = document.getElementById('password-error');

    expect(passwordErrorRow).not.toBeNull();
    expect(passwordErrorRow).toHaveTextContent('Password does not meet requirements.');

    const toggleButton = screen.getByRole('button', { name: /show password/i });
    expect(toggleButton).toHaveTextContent('👁');
    await user.click(toggleButton);

    expect(screen.getByLabelText(/password/i, { selector: 'input#password' })).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: /hide password/i })).toHaveTextContent('🙈');
    expect(passwordErrorRow).toBeInTheDocument();
    expect(screen.getByText('Minimum 8 characters, at least one uppercase letter and one number.')).toBeInTheDocument();
  });

  it('uses icon-only toggle for confirm password visibility', async () => {
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    const confirmToggleButton = screen.getByRole('button', { name: /show confirm password/i });
    expect(confirmToggleButton).toHaveTextContent('👁');

    await user.click(confirmToggleButton);

    expect(screen.getByRole('button', { name: /hide confirm password/i })).toHaveTextContent('🙈');
  });

  it('renders field-level error icon before error text', async () => {
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    await user.click(screen.getByRole('button', { name: /create account/i }));

    const firstNameErrorMessage = screen.getByText('First name is required.');
    const firstNameErrorRow = firstNameErrorMessage.closest('p');

    expect(firstNameErrorRow).not.toBeNull();

    const firstNameErrorIcon = firstNameErrorRow?.firstElementChild as HTMLElement | null;

    expect(firstNameErrorIcon).not.toBeNull();
    if (!firstNameErrorIcon) {
      throw new Error('Expected first name error icon to be rendered');
    }
    expect(firstNameErrorIcon).toHaveClass(styles.fieldErrorIcon);
    expect(firstNameErrorIcon).toHaveClass(styles.fieldErrorIconTriangle);
    expect(firstNameErrorIcon).toHaveTextContent('!');
    expect(firstNameErrorIcon.compareDocumentPosition(firstNameErrorMessage) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('does not render label-level warning indicators for required registration fields', () => {
    render(
      <BrowserRouter>
        <RegistrationForm />
      </BrowserRouter>
    );

    expect(screen.queryByTestId('required-indicator')).not.toBeInTheDocument();
  });
});
