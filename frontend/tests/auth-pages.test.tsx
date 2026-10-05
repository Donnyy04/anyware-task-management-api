import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { Login, Register } from '../src/pages';

const mockAuth = vi.hoisted(() => ({
  user: null,
  loading: false,
  message: '',
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  clearMessage: vi.fn(),
}));

vi.mock('../src/auth', () => ({ useAuth: () => mockAuth }));

function renderRoutes(initialPath: string, page: React.ReactNode) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/login" element={page} />
        <Route path="/register" element={page} />
        <Route path="/tasks" element={<h1>My tasks</h1>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('authentication forms', () => {
  beforeEach(() => {
    mockAuth.login.mockReset();
    mockAuth.register.mockReset();
  });

  afterEach(cleanup);

  it('validates required login fields before making an API request', async () => {
    const user = userEvent.setup();
    renderRoutes('/login', <Login />);

    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument();
    expect(screen.getByText('Password is required.')).toBeInTheDocument();
    expect(mockAuth.login).not.toHaveBeenCalled();
  });

  it('shows a readable server error on login', async () => {
    const user = userEvent.setup();
    mockAuth.login.mockRejectedValueOnce(new Error('Invalid email or password.'));
    renderRoutes('/login', <Login />);

    await user.type(screen.getByLabelText('Email address'), 'donny@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-horse');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
  });

  it('rejects mismatched registration passwords before submitting', async () => {
    const user = userEvent.setup();
    renderRoutes('/register', <Register />);

    await user.type(screen.getByLabelText('Your name'), 'Donny');
    await user.type(screen.getByLabelText('Email address'), 'donny@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-horse');
    await user.type(screen.getByLabelText('Confirm password'), 'different-horse');
    await user.click(screen.getByRole('button', { name: /create account/i }));

    expect(await screen.findByText('Passwords do not match.')).toBeInTheDocument();
    expect(mockAuth.register).not.toHaveBeenCalled();
  });

  it('navigates to tasks after successful login', async () => {
    const user = userEvent.setup();
    mockAuth.login.mockResolvedValueOnce(undefined);
    renderRoutes('/login', <Login />);

    await user.type(screen.getByLabelText('Email address'), 'donny@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-horse');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('heading', { name: 'My tasks' })).toBeInTheDocument();
    expect(mockAuth.login).toHaveBeenCalledWith({ email: 'donny@example.com', password: 'correct-horse' });
  });
});
