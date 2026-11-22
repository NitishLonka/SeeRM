import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../api/client';
import { formatCurrency, formatRelativeTime } from '../../utils/format';

const SEARCH_DEBOUNCE_MS = 300;

export default function CrmHomepage() {
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState(null);

  const [customers, setCustomers] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [customersLoading, setCustomersLoading] = useState(true);
  const [customersError, setCustomersError] = useState(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  // KPI cards
  useEffect(() => {
    const controller = new AbortController();
    api
      .getDashboardSummary(controller.signal)
      .then(setSummary)
      .catch((err) => err.name !== 'AbortError' && setSummaryError(err.message));
    return () => controller.abort();
  }, []);

  // Wait for typing to pause before searching on the server.
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchQuery.trim()), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Customer directory (server-side search by name or company)
  useEffect(() => {
    const controller = new AbortController();
    setCustomersLoading(true);
    setCustomersError(null);
    api
      .listCustomers({ search: debouncedSearch }, controller.signal)
      .then((result) => {
        setCustomers(result.items);
        setTotalCount(result.totalCount);
        setCustomersLoading(false);
      })
      .catch((err) => {
        if (err.name === 'AbortError') return;
        setCustomersError(err.message);
        setCustomersLoading(false);
      });
    return () => controller.abort();
  }, [debouncedSearch]);

  const kpi = (value) => (summary ? value(summary) : summaryError ? '—' : '…');
  const completionRate =
    summary && summary.tasksTotal > 0 ? Math.round((summary.tasksCompleted / summary.tasksTotal) * 100) : 0;

  return (
    <div style={{ fontFamily: 'sans-serif', backgroundColor: '#f8fafc', minHeight: '100vh', padding: '24px' }}>
      {/* Top Header */}
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '1.5rem', color: '#0f172a' }}>CRM Dashboard</h1>
          <p style={{ margin: '4px 0 0', color: '#64748b', fontSize: '0.875rem' }}>
            Welcome back. Here is your pipeline overview.
          </p>
        </div>
        <Link
          to="/customer-entry"
          style={{
            backgroundColor: '#2563eb',
            color: '#fff',
            textDecoration: 'none',
            padding: '10px 16px',
            borderRadius: '6px',
            fontWeight: '600',
          }}
        >
          + New Customer
        </Link>
      </header>

      {/* KPI Metrics Row */}
      <section style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', marginBottom: '24px' }}>
        <StatCard title="Total Customers" value={kpi((s) => s.totalCustomers)} />
        <StatCard
          title="Active Leads"
          value={kpi((s) => s.activeLeads)}
          trend={summary && `${summary.followUpsDue} follow-up${summary.followUpsDue === 1 ? '' : 's'} due`}
        />
        <StatCard title="Pipeline Revenue" value={kpi((s) => formatCurrency(s.pipelineRevenue))} />
        <StatCard
          title="Tasks Completed"
          value={kpi((s) => `${s.tasksCompleted} / ${s.tasksTotal}`)}
          trend={summary && `${completionRate}% completion rate`}
        />
      </section>
      {summaryError && <ErrorBanner message={`Couldn't load dashboard metrics: ${summaryError}`} />}

      {/* Main Content Area: Recent Customers Table */}
      <section style={{ backgroundColor: '#fff', borderRadius: '8px', padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '1.125rem', color: '#0f172a' }}>
            Customer Directory
            {!customersLoading && !customersError && (
              <span style={{ marginLeft: '8px', fontSize: '0.875rem', fontWeight: 'normal', color: '#64748b' }}>
                ({totalCount})
              </span>
            )}
          </h2>
          <input
            type="text"
            placeholder="Search name or company..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{
              padding: '8px 12px',
              borderRadius: '6px',
              border: '1px solid #cbd5e1',
              width: '250px',
            }}
          />
        </div>

        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #e2e8f0', color: '#64748b', fontSize: '0.875rem' }}>
              <th style={{ padding: '12px 8px' }}>Customer</th>
              <th style={{ padding: '12px 8px' }}>Company</th>
              <th style={{ padding: '12px 8px' }}>Status</th>
              <th style={{ padding: '12px 8px' }}>Deal Value</th>
              <th style={{ padding: '12px 8px' }}>Last Contact</th>
            </tr>
          </thead>
          <tbody>
            {!customersError &&
              customers.map((customer) => (
                <tr
                  key={customer.id}
                  style={{
                    borderBottom: '1px solid #f1f5f9',
                    fontSize: '0.925rem',
                    opacity: customersLoading ? 0.5 : 1,
                  }}
                >
                  <td style={{ padding: '12px 8px', fontWeight: '500', color: '#0f172a' }}>{customer.name}</td>
                  <td style={{ padding: '12px 8px', color: '#475569' }}>{customer.company || '—'}</td>
                  <td style={{ padding: '12px 8px' }}>
                    <StatusBadge status={customer.status} />
                  </td>
                  <td style={{ padding: '12px 8px', color: '#0f172a' }}>{formatCurrency(customer.dealValue)}</td>
                  <td
                    style={{ padding: '12px 8px', color: '#64748b' }}
                    title={customer.lastContactAt ? new Date(customer.lastContactAt).toLocaleString() : undefined}
                  >
                    {formatRelativeTime(customer.lastContactAt)}
                  </td>
                </tr>
              ))}
            {customersLoading && customers.length === 0 && !customersError && (
              <MessageRow>Loading customers…</MessageRow>
            )}
            {customersError && <MessageRow color="#b91c1c">Couldn't load customers: {customersError}</MessageRow>}
            {!customersLoading && !customersError && customers.length === 0 && (
              <MessageRow>No matching customers found.</MessageRow>
            )}
          </tbody>
        </table>
      </section>

    </div>
  );
}

// Reusable KPI Card Component
function StatCard({ title, value, trend }) {
  return (
    <div style={{ backgroundColor: '#fff', padding: '16px', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
      <p style={{ margin: 0, fontSize: '0.875rem', color: '#64748b' }}>{title}</p>
      <h3 style={{ margin: '8px 0 4px', fontSize: '1.5rem', color: '#0f172a' }}>{value}</h3>
      {trend && <span style={{ fontSize: '0.75rem', color: '#16a34a' }}>{trend}</span>}
    </div>
  );
}

function MessageRow({ children, color = '#64748b' }) {
  return (
    <tr>
      <td colSpan="5" style={{ padding: '20px', textAlign: 'center', color }}>
        {children}
      </td>
    </tr>
  );
}

function ErrorBanner({ message }) {
  return (
    <div
      role="alert"
      style={{
        backgroundColor: '#fef2f2',
        color: '#b91c1c',
        border: '1px solid #fecaca',
        borderRadius: '6px',
        padding: '10px 14px',
        marginBottom: '24px',
        fontSize: '0.875rem',
      }}
    >
      {message}
    </div>
  );
}

// Reusable Status Badge Component
function StatusBadge({ status }) {
  const colors = {
    Active: { bg: '#dcfce7', text: '#166534' },
    Lead: { bg: '#fef9c3', text: '#854d0e' },
    Inactive: { bg: '#f1f5f9', text: '#475569' },
  };
  const style = colors[status] || colors.Inactive;

  return (
    <span
      style={{
        backgroundColor: style.bg,
        color: style.text,
        padding: '4px 10px',
        borderRadius: '9999px',
        fontSize: '0.75rem',
        fontWeight: '600',
      }}
    >
      {status}
    </span>
  );
}
