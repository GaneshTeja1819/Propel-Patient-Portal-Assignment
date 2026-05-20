import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import SessionTimeoutModal from './SessionTimeoutModal';

describe('SessionTimeoutModal', () => {
  it('renders warning dialog with assertive countdown and focusable CTA', async () => {
    const user = userEvent.setup();
    const onStayLoggedIn = vi.fn();

    render(
      <SessionTimeoutModal
        secondsRemaining={120}
        onStayLoggedIn={onStayLoggedIn}
        refreshFailed={false}
        expired={false}
      />
    );

    expect(screen.getByRole('dialog')).toHaveAttribute('aria-modal', 'true');
    expect(screen.getByText('Session expiring in 120 seconds')).toBeInTheDocument();
    expect(screen.getByText('Session expiring in 2 minutes')).toBeInTheDocument();

    await user.tab();
    expect(screen.getByRole('button', { name: 'Stay logged in' })).toHaveFocus();
  });

  it('renders expired state without CTA', () => {
    render(
      <SessionTimeoutModal
        secondsRemaining={0}
        onStayLoggedIn={vi.fn()}
        refreshFailed={false}
        expired
      />
    );

    expect(screen.getByText('Session expired. Redirecting to login...')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Stay logged in' })).not.toBeInTheDocument();
  });
});
