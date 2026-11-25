import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../../App';
import { api, ApiError } from '../../api/client';

jest.mock('../../api/client', () => ({
  ...jest.requireActual('../../api/client'),
  api: {
    getDashboardSummary: jest.fn(),
    listCustomers: jest.fn(),
    createCustomer: jest.fn(),
    getLeadSummary: jest.fn(),
    listLeads: jest.fn(),
    getLead: jest.fn(),
    createLead: jest.fn(),
    updateLead: jest.fn(),
    deleteLead: jest.fn(),
    convertLead: jest.fn(),
  },
}));

const lead = (overrides) => ({
  id: 1,
  firstName: 'Olivia',
  lastName: 'Bennett',
  fullName: 'Olivia Bennett',
  email: 'olivia@tidewater.example',
  phone: '555-301-2001',
  company: 'Tidewater Freight',
  jobTitle: 'Ops Manager',
  source: 'website',
  status: 'New',
  score: 35,
  estimatedValue: 12000,
  notes: null,
  convertedCustomerId: null,
  convertedAt: null,
  createdAt: new Date(Date.now() - 24 * 3600 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
});

const NEW_LEAD = lead();
const QUALIFIED_LEAD = lead({ id: 14, firstName: 'Nora', lastName: 'Lindqvist', fullName: 'Nora Lindqvist', email: 'nora@polaris.example', company: 'Polaris Energy', status: 'Qualified', score: 80, estimatedValue: 42000 });
const CONVERTED_LEAD = lead({ id: 23, firstName: 'Tom', lastName: 'Okafor', fullName: 'Tom Okafor', email: 'tom@mail.example', company: null, status: 'Converted', convertedCustomerId: 6 });

function renderAt(path) {
  window.history.pushState({}, '', path);
  return render(<App />);
}

beforeEach(() => {
  jest.clearAllMocks();
  api.getLeadSummary.mockResolvedValue({ totalLeads: 25, newThisWeek: 6, qualified: 6, converted: 3, conversionRate: 12, openPipelineValue: 251750 });
  api.listLeads.mockResolvedValue({ items: [NEW_LEAD, QUALIFIED_LEAD, CONVERTED_LEAD], page: 1, pageSize: 20, totalCount: 3 });
});

test('leads screen shows KPIs, rows and the right actions per status', async () => {
  renderAt('/leads');

  expect(await screen.findByText('Nora Lindqvist')).toBeInTheDocument();
  expect(screen.getByText('$251,750')).toBeInTheDocument();
  expect(screen.getByText('12%')).toBeInTheDocument();

  const rowOf = (name) => screen.getByText(name).closest('tr');
  expect(within(rowOf('Nora Lindqvist')).getByRole('button', { name: 'Convert to Customer' })).toBeInTheDocument();
  expect(within(rowOf('Olivia Bennett')).queryByRole('button', { name: 'Convert to Customer' })).not.toBeInTheDocument();
  expect(within(rowOf('Olivia Bennett')).getByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/leads/1/edit');
  expect(within(rowOf('Tom Okafor')).getByText('Customer #6')).toBeInTheDocument();
  expect(within(rowOf('Tom Okafor')).queryByRole('link', { name: 'Edit' })).not.toBeInTheDocument();
});

test('status tab filters through the API', async () => {
  renderAt('/leads');
  await screen.findByText('Nora Lindqvist');

  await userEvent.click(screen.getByRole('tab', { name: 'Qualified' }));
  await waitFor(() => expect(api.listLeads).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Qualified', page: 1 }), expect.anything()));
});

test('converting a qualified lead shows the result and reloads', async () => {
  api.convertLead.mockResolvedValue({ lead: { ...QUALIFIED_LEAD, status: 'Converted' }, customerId: 24, customerCreated: true, dealId: 32, warnings: [] });
  renderAt('/leads');
  await screen.findByText('Nora Lindqvist');
  const callsBefore = api.listLeads.mock.calls.length;

  await userEvent.click(screen.getByRole('button', { name: 'Convert to Customer' }));

  expect(api.convertLead).toHaveBeenCalledWith(14);
  expect(await screen.findByText(/converted to a new customer/)).toHaveTextContent('customer #24');
  expect(screen.getByText(/converted to a new customer/)).toHaveTextContent('$42,000');
  await waitFor(() => expect(api.listLeads.mock.calls.length).toBeGreaterThan(callsBefore));
});

test('conversion failure (Customer service down) shows the API message', async () => {
  api.convertLead.mockRejectedValue(new ApiError(503, { detail: 'The Customer service is unavailable. The lead was not converted; please try again.' }));
  renderAt('/leads');
  await screen.findByText('Nora Lindqvist');

  await userEvent.click(screen.getByRole('button', { name: 'Convert to Customer' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Customer service is unavailable');
});

test('new lead form submits and returns to the list; duplicate email shows under the field', async () => {
  api.createLead.mockRejectedValueOnce(new ApiError(409, { detail: "A lead with email 'x@y.example' already exists." }));
  api.createLead.mockResolvedValueOnce(lead({ id: 26 }));
  renderAt('/leads/new');

  await userEvent.type(screen.getByLabelText('First Name *'), 'Test');
  await userEvent.type(screen.getByLabelText('Last Name *'), 'Lead');
  await userEvent.type(screen.getByLabelText('Email Address *'), 'x@y.example');
  await userEvent.type(screen.getByLabelText('Estimated Value ($)'), '5000');
  await userEvent.click(screen.getByRole('button', { name: 'Create Lead' }));

  expect(await screen.findByText('A lead with this email already exists')).toBeInTheDocument();
  expect(api.createLead).toHaveBeenCalledWith(expect.objectContaining({ firstName: 'Test', estimatedValue: 5000, score: 0, status: 'New' }));

  await userEvent.click(screen.getByRole('button', { name: 'Create Lead' }));
  expect(await screen.findByRole('heading', { name: 'Leads' })).toBeInTheDocument();
});
