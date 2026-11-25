// Thin fetch wrapper for the SeeRM API gateway (backend/gateway), which routes to each microservice.
const BASE_URL = (process.env.REACT_APP_API_BASE_URL || 'http://localhost:5000').replace(/\/$/, '');

/** Error carrying the HTTP status and the RFC 7807 ProblemDetails body returned by the API. */
export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** Field validation errors keyed by camelCase field name, e.g. { email: 'Please enter a valid email address' }. */
  get fieldErrors() {
    const errors = this.problem?.errors || {};
    return Object.fromEntries(
      Object.entries(errors).map(([key, messages]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        Array.isArray(messages) ? messages[0] : String(messages),
      ])
    );
  }
}

async function request(path, { method = 'GET', body, signal } = {}) {
  let response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      signal,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (err) {
    if (err.name === 'AbortError') throw err;
    throw new ApiError(0, { title: `Cannot reach the API at ${BASE_URL}. Is the backend running?` });
  }

  if (response.status === 204) return null;

  const isJson = response.headers.get('content-type')?.includes('json');
  const data = isJson ? await response.json() : null;
  if (!response.ok) throw new ApiError(response.status, data);
  return data;
}

export const api = {
  getDashboardSummary: (signal) => request('/api/dashboard/summary', { signal }),

  listCustomers: ({ search, status, page = 1, pageSize = 50 } = {}, signal) => {
    const params = new URLSearchParams({ page, pageSize });
    if (search) params.set('search', search);
    if (status) params.set('status', status);
    return request(`/api/customers?${params}`, { signal });
  },

  createCustomer: (customer) => request('/api/customers', { method: 'POST', body: customer }),

  // ---- Leads service (routed by the gateway) ----
  getLeadSummary: (signal) => request('/api/leads/summary', { signal }),

  listLeads: ({ search, status, source, page = 1, pageSize = 20 } = {}, signal) => {
    const params = new URLSearchParams({ page, pageSize });
    if (search) params.set('search', search);
    if (status) params.set('status', status);
    if (source) params.set('source', source);
    return request(`/api/leads?${params}`, { signal });
  },

  getLead: (id, signal) => request(`/api/leads/${id}`, { signal }),
  createLead: (lead) => request('/api/leads', { method: 'POST', body: lead }),
  updateLead: (id, lead) => request(`/api/leads/${id}`, { method: 'PUT', body: lead }),
  deleteLead: (id) => request(`/api/leads/${id}`, { method: 'DELETE' }),

  /** Converts a Qualified lead into a customer in the Customer service. */
  convertLead: (id) => request(`/api/leads/${id}/convert`, { method: 'POST' }),
};
