import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../api/client';
import { formatCurrency, formatRelativeTime } from '../../utils/format';
import {
  Banner,
  ErrorBanner,
  MessageRow,
  StatCard,
  cardStyle,
  pageStyle,
  primaryButtonStyle,
  secondaryLinkStyle,
} from '../../components/ui';
import { LEAD_SOURCES, LEAD_STATUSES, LeadStatusBadge, ScoreBar, sourceLabel } from './lead-constants';

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE = 20;
const COLUMNS = 8;

const cell = { padding: '12px 8px' };
const actionButton = {
  background: 'none',
  border: '1px solid #cbd5e1',
  borderRadius: '6px',
  padding: '4px 10px',
  fontSize: '0.8rem',
  cursor: 'pointer',
  color: '#334155',
  textDecoration: 'none',
  whiteSpace: 'nowrap',
};

export default function LeadsPage() {
  // KPI cards
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState(null);

  // Table + filters
  const [status, setStatus] = useState('');
  const [source, setSource] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [page, setPage] = useState(1);
  const [leads, setLeads] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  // Row actions
  const [busyId, setBusyId] = useState(null);
  const [notice, setNotice] = useState(null); // { kind, content }
  const [reloadKey, setReloadKey] = useState(0);
  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    const controller = new AbortController();
    setSummaryError(null);
    api
      .getLeadSummary(controller.signal)
      .then(setSummary)
      .catch((err) => err.name !== 'AbortError' && setSummaryError(err.message));
    return () => controller.abort();
  }, [reloadKey]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchQuery.trim()), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Any filter change goes back to page 1.
  useEffect(() => setPage(1), [status, source, debouncedSearch]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setLoadError(null);
    api
      .listLeads({ search: debouncedSearch, status, source, page, pageSize: PAGE_SIZE }, controller.signal)
      .then((result) => {
        setLeads(result.items);
        setTotalCount(result.totalCount);
        setLoading(false);
      })
      .catch((err) => {
        if (err.name === 'AbortError') return;
        setLoadError(err.message);
        setLoading(false);
      });
    return () => controller.abort();
  }, [debouncedSearch, status, source, page, reloadKey]);

  const handleConvert = async (lead) => {
    setBusyId(lead.id);
    setNotice(null);
    try {
      const result = await api.convertLead(lead.id);
      setNotice({
        kind: result.warnings.length ? 'warning' : 'success',
        content: (
          <>
            <strong>{lead.fullName}</strong> was converted{' '}
            {result.customerCreated ? 'to a new customer' : 'and linked to an existing customer with the same email'}{' '}
            (customer #{result.customerId})
            {result.dealId && <> with an open deal of {formatCurrency(lead.estimatedValue)}</>}.{' '}
            <Link to="/" style={{ color: 'inherit', fontWeight: 600 }}>
              View customers
            </Link>
            {result.warnings.map((w) => (
              <div key={w} style={{ marginTop: '4px' }}>
                ⚠ {w}
              </div>
            ))}
          </>
        ),
      });
      reload();
    } catch (err) {
      setNotice({ kind: 'error', content: <>Couldn't convert {lead.fullName}: {err.message}</> });
    } finally {
      setBusyId(null);
    }
  };

  const handleDelete = async (lead) => {
    if (!window.confirm(`Delete lead "${lead.fullName}"? This can't be undone.`)) return;
    setBusyId(lead.id);
    setNotice(null);
    try {
      await api.deleteLead(lead.id);
      setNotice({ kind: 'success', content: <>Lead {lead.fullName} was deleted.</> });
      reload();
    } catch (err) {
      setNotice({ kind: 'error', content: <>Couldn't delete {lead.fullName}: {err.message}</> });
    } finally {
      setBusyId(null);
    }
  };

  const kpi = (fn) => (summary ? fn(summary) : summaryError ? '—' : '…');
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const firstRow = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const lastRow = Math.min(page * PAGE_SIZE, totalCount);

  return (
    <div style={pageStyle}>
      <header
        style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px', gap: '12px', flexWrap: 'wrap' }}
      >
        <div>
          <Link to="/" style={{ fontSize: '0.875rem', color: '#2563eb', textDecoration: 'none' }}>
            &larr; Back to Dashboard
          </Link>
          <h1 style={{ margin: '4px 0 0', fontSize: '1.5rem', color: '#0f172a' }}>Leads</h1>
          <p style={{ margin: '4px 0 0', color: '#64748b', fontSize: '0.875rem' }}>
            Capture and qualify prospects, then convert them into customers.
          </p>
        </div>
        <nav style={{ display: 'flex', gap: '8px' }}>
          <Link to="/" style={secondaryLinkStyle}>
            Customers
          </Link>
          <Link to="/leads/new" style={primaryButtonStyle}>
            + New Lead
          </Link>
        </nav>
      </header>

      <section style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', marginBottom: '24px' }}>
        <StatCard title="New This Week" value={kpi((s) => s.newThisWeek)} trend={summary && `${summary.totalLeads} leads in total`} />
        <StatCard title="Qualified" value={kpi((s) => s.qualified)} trend={summary && 'Ready to convert'} />
        <StatCard
          title="Conversion Rate"
          value={kpi((s) => `${s.conversionRate}%`)}
          trend={summary && `${summary.converted} converted`}
        />
        <StatCard title="Open Pipeline" value={kpi((s) => formatCurrency(s.openPipelineValue))} trend={summary && 'New, Contacted & Qualified'} />
      </section>
      {summaryError && <ErrorBanner message={`Couldn't load lead metrics: ${summaryError}`} />}

      {notice && (
        <Banner kind={notice.kind} onDismiss={() => setNotice(null)}>
          {notice.content}
        </Banner>
      )}

      <section style={cardStyle}>
        {/* Status tabs */}
        <div role="tablist" aria-label="Filter by status" style={{ display: 'flex', gap: '4px', flexWrap: 'wrap', borderBottom: '1px solid #e2e8f0', marginBottom: '16px' }}>
          {['', ...LEAD_STATUSES].map((s) => {
            const active = status === s;
            return (
              <button
                key={s || 'all'}
                role="tab"
                aria-selected={active}
                onClick={() => setStatus(s)}
                style={{
                  background: 'none',
                  border: 'none',
                  borderBottom: active ? '2px solid #2563eb' : '2px solid transparent',
                  color: active ? '#1d4ed8' : '#64748b',
                  fontWeight: active ? 600 : 500,
                  padding: '8px 12px',
                  cursor: 'pointer',
                  fontSize: '0.9rem',
                }}
              >
                {s || 'All'}
              </button>
            );
          })}
        </div>

        {/* Search + source filter */}
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', marginBottom: '16px' }}>
          <input
            type="search"
            aria-label="Search leads"
            placeholder="Search name, company or email..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{ padding: '8px 12px', borderRadius: '6px', border: '1px solid #cbd5e1', flex: '1 1 240px', maxWidth: '360px' }}
          />
          <select
            aria-label="Filter by source"
            value={source}
            onChange={(e) => setSource(e.target.value)}
            style={{ padding: '8px 12px', borderRadius: '6px', border: '1px solid #cbd5e1' }}
          >
            <option value="">All sources</option>
            {LEAD_SOURCES.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>

        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', minWidth: '860px' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid #e2e8f0', color: '#64748b', fontSize: '0.875rem' }}>
                <th style={cell}>Lead</th>
                <th style={cell}>Company</th>
                <th style={cell}>Source</th>
                <th style={cell}>Status</th>
                <th style={cell}>Score</th>
                <th style={cell}>Est. Value</th>
                <th style={cell}>Created</th>
                <th style={{ ...cell, textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {!loadError &&
                leads.map((lead) => {
                  const busy = busyId === lead.id;
                  return (
                    <tr
                      key={lead.id}
                      style={{ borderBottom: '1px solid #f1f5f9', fontSize: '0.925rem', opacity: loading || busy ? 0.5 : 1 }}
                    >
                      <td style={cell}>
                        <div style={{ fontWeight: 500, color: '#0f172a' }}>{lead.fullName}</div>
                        <div style={{ fontSize: '0.8rem', color: '#64748b' }}>{lead.email}</div>
                      </td>
                      <td style={{ ...cell, color: '#475569' }}>
                        {lead.company || '—'}
                        {lead.jobTitle && <div style={{ fontSize: '0.8rem', color: '#94a3b8' }}>{lead.jobTitle}</div>}
                      </td>
                      <td style={{ ...cell, color: '#475569' }}>{sourceLabel(lead.source)}</td>
                      <td style={cell}>
                        <LeadStatusBadge status={lead.status} />
                      </td>
                      <td style={cell}>
                        <ScoreBar score={lead.score} />
                      </td>
                      <td style={{ ...cell, color: '#0f172a' }}>
                        {lead.estimatedValue != null ? formatCurrency(lead.estimatedValue) : '—'}
                      </td>
                      <td style={{ ...cell, color: '#64748b' }} title={new Date(lead.createdAt).toLocaleString()}>
                        {formatRelativeTime(lead.createdAt)}
                      </td>
                      <td style={{ ...cell, textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '6px' }}>
                          {lead.status === 'Qualified' && (
                            <button
                              type="button"
                              disabled={busy}
                              onClick={() => handleConvert(lead)}
                              style={{ ...actionButton, backgroundColor: '#2563eb', borderColor: '#2563eb', color: '#fff' }}
                            >
                              {busy ? 'Converting…' : 'Convert to Customer'}
                            </button>
                          )}
                          {lead.status === 'Converted' ? (
                            <span style={{ fontSize: '0.8rem', color: '#166534', alignSelf: 'center' }}>
                              Customer #{lead.convertedCustomerId}
                            </span>
                          ) : (
                            <Link to={`/leads/${lead.id}/edit`} style={actionButton}>
                              Edit
                            </Link>
                          )}
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() => handleDelete(lead)}
                            style={{ ...actionButton, color: '#b91c1c', borderColor: '#fecaca' }}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              {loading && leads.length === 0 && !loadError && <MessageRow colSpan={COLUMNS}>Loading leads…</MessageRow>}
              {loadError && (
                <MessageRow colSpan={COLUMNS} color="#b91c1c">
                  Couldn't load leads: {loadError}
                </MessageRow>
              )}
              {!loading && !loadError && leads.length === 0 && <MessageRow colSpan={COLUMNS}>No matching leads found.</MessageRow>}
            </tbody>
          </table>
        </div>

        {!loadError && totalCount > 0 && (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '12px', fontSize: '0.875rem', color: '#64748b' }}>
            <span>
              Showing {firstRow}–{lastRow} of {totalCount}
            </span>
            {totalPages > 1 && (
              <div style={{ display: 'flex', gap: '6px' }}>
                <button type="button" style={actionButton} disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                  Previous
                </button>
                <button type="button" style={actionButton} disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                  Next
                </button>
              </div>
            )}
          </div>
        )}
      </section>
    </div>
  );
}
