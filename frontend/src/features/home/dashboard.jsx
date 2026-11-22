import React, { useState } from 'react';
import { Link } from 'react-router-dom';

const INITIAL_CUSTOMERS = [
  { id: 1, name: 'Sarah Jenkins', company: 'Apex Logistics', status: 'Active', value: '$14,500', lastContact: '2 hours ago' },
  { id: 2, name: 'Marcus Vance', company: 'Brightline Tech', status: 'Lead', value: '$8,200', lastContact: '1 day ago' },
  { id: 3, name: 'Elena Rostova', company: 'Horizon Retail', status: 'Active', value: '$32,000', lastContact: '3 days ago' },
  { id: 4, name: 'David Kim', company: 'Kestrel Media', status: 'Inactive', value: '$4,100', lastContact: '2 weeks ago' },
];

export default function CrmHomepage() {
  const [customers] = useState(INITIAL_CUSTOMERS);
  const [searchQuery, setSearchQuery] = useState('');

  // Filter customers by name or company
  const filteredCustomers = customers.filter(
    (c) =>
      c.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      c.company.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Dynamic KPI calculations
  const totalCustomers = customers.length;
  const activeLeads = customers.filter((c) => c.status === 'Lead').length;

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
        <StatCard title="Total Customers" value={totalCustomers} trend="+12% this month" />
        <StatCard title="Active Leads" value={activeLeads} trend="3 follow-ups due" />
        <StatCard title="Pipeline Revenue" value="$58,800" trend="+8.4% vs last quarter" />
        <StatCard title="Tasks Completed" value="18 / 22" trend="82% completion rate" />
      </section>

      {/* Main Content Area: Recent Customers Table */}
      <section style={{ backgroundColor: '#fff', borderRadius: '8px', padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ margin: 0, fontSize: '1.125rem', color: '#0f172a' }}>Customer Directory</h2>
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
            {filteredCustomers.map((customer) => (
              <tr key={customer.id} style={{ borderBottom: '1px solid #f1f5f9', fontSize: '0.925rem' }}>
                <td style={{ padding: '12px 8px', fontWeight: '500', color: '#0f172a' }}>{customer.name}</td>
                <td style={{ padding: '12px 8px', color: '#475569' }}>{customer.company}</td>
                <td style={{ padding: '12px 8px' }}>
                  <StatusBadge status={customer.status} />
                </td>
                <td style={{ padding: '12px 8px', color: '#0f172a' }}>{customer.value}</td>
                <td style={{ padding: '12px 8px', color: '#64748b' }}>{customer.lastContact}</td>
              </tr>
            ))}
            {filteredCustomers.length === 0 && (
              <tr>
                <td colSpan="5" style={{ padding: '20px', textAlign: 'center', color: '#64748b' }}>
                  No matching customers found.
                </td>
              </tr>
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
      <span style={{ fontSize: '0.75rem', color: '#16a34a' }}>{trend}</span>
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
