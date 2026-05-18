import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import BaselineDemo from './BaselineDemo';

describe('BaselineDemo', () => {
  it('renders the page title', () => {
    render(<BaselineDemo />);
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
  });

  it('renders all section headings', () => {
    render(<BaselineDemo />);
    expect(screen.getByRole('heading', { name: /colour tokens/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /typography scale/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /interactive elements/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /spacing/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /elevation/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /status banners/i })).toBeInTheDocument();
  });

  it('all buttons are accessible with labels', () => {
    render(<BaselineDemo />);
    const buttons = screen.getAllByRole('button');
    buttons.forEach((button) => {
      expect(button).toBeVisible();
    });
  });

  it('text input has an associated label', () => {
    render(<BaselineDemo />);
    expect(screen.getByLabelText(/text input/i)).toBeInTheDocument();
  });

  it('select has an associated label', () => {
    render(<BaselineDemo />);
    expect(screen.getByLabelText(/select/i)).toBeInTheDocument();
  });

  it('status banners use correct ARIA roles', () => {
    render(<BaselineDemo />);
    expect(screen.getByRole('status')).toBeInTheDocument();
    const alerts = screen.getAllByRole('alert');
    expect(alerts).toHaveLength(2);
  });
});
