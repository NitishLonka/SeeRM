import { render, screen, waitFor } from '@testing-library/react';
import App from './App';
import { api } from './api/client';
import { formatCurrency, formatRelativeTime } from './utils/format';

jest.mock('./api/client', () => ({
  ...jest.requireActual('./api/client'),
  api: {
    getDashboardSummary: jest.fn(),
    listCustomers: jest.fn(),
    createCustomer: jest.fn(),
  },
}));

beforeEach(() => {
  api.getDashboardSummary.mockResolvedValue({
    totalCustomers: 20,
    activeLeads: 7,
    pipelineRevenue: 146850,
    tasksCompleted: 18,
    tasksTotal: 22,
    followUpsDue: 3,
  });
  api.listCustomers.mockResolvedValue({
    items: [
      {
        id: 1,
        name: 'Sarah Jenkins',
        company: 'Apex Logistics',
        status: 'Active',
        dealValue: 14500,
        lastContactAt: new Date(Date.now() - 2 * 3600 * 1000).toISOString(),
      },
    ],
    page: 1,
    pageSize: 50,
    totalCount: 1,
  });
});

test('dashboard renders KPIs and customers from the API', async () => {
  render(<App />);

  expect(await screen.findByText('Sarah Jenkins')).toBeInTheDocument();
  expect(screen.getByText('$14,500')).toBeInTheDocument();
  expect(screen.getByText('2 hours ago')).toBeInTheDocument();
  expect(screen.getByText('$146,850')).toBeInTheDocument();
  expect(screen.getByText('18 / 22')).toBeInTheDocument();
  expect(screen.getByText('3 follow-ups due')).toBeInTheDocument();
  await waitFor(() => expect(api.listCustomers).toHaveBeenCalled());
});

test('dashboard shows an error when the API is unreachable', async () => {
  api.listCustomers.mockRejectedValue(new Error('Cannot reach the API'));
  render(<App />);
  expect(await screen.findByText(/Couldn't load customers/)).toBeInTheDocument();
});

test('formatters', () => {
  expect(formatCurrency(32000)).toBe('$32,000');
  expect(formatRelativeTime(null)).toBe('Never');
  const now = Date.now();
  expect(formatRelativeTime(new Date(now - 24 * 3600 * 1000).toISOString(), now)).toBe('1 day ago');
  expect(formatRelativeTime(new Date(now - 14 * 24 * 3600 * 1000).toISOString(), now)).toBe('2 weeks ago');
});
