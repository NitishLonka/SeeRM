// Values mirror the Leads service (sqlscripts/leads/001_create_tables.sql).

export const LEAD_STATUSES = ['New', 'Contacted', 'Qualified', 'Unqualified', 'Converted'];
/** Statuses a user can set directly; "Converted" is only reachable through Convert to Customer. */
export const EDITABLE_LEAD_STATUSES = ['New', 'Contacted', 'Qualified', 'Unqualified'];

export const LEAD_SOURCES = [
  { value: 'website', label: 'Website' },
  { value: 'referral', label: 'Referral' },
  { value: 'event', label: 'Event' },
  { value: 'cold_call', label: 'Cold call' },
  { value: 'social', label: 'Social' },
  { value: 'other', label: 'Other' },
];

export const sourceLabel = (value) => LEAD_SOURCES.find((s) => s.value === value)?.label ?? value;

const STATUS_COLORS = {
  New: { bg: '#dbeafe', text: '#1e40af' },
  Contacted: { bg: '#e0e7ff', text: '#3730a3' },
  Qualified: { bg: '#fef9c3', text: '#854d0e' },
  Unqualified: { bg: '#f1f5f9', text: '#475569' },
  Converted: { bg: '#dcfce7', text: '#166534' },
};

export function LeadStatusBadge({ status }) {
  const c = STATUS_COLORS[status] || STATUS_COLORS.Unqualified;
  return (
    <span
      style={{
        backgroundColor: c.bg,
        color: c.text,
        padding: '4px 10px',
        borderRadius: '9999px',
        fontSize: '0.75rem',
        fontWeight: '600',
        whiteSpace: 'nowrap',
      }}
    >
      {status}
    </span>
  );
}

/** 0–100 score as a small bar plus the number. */
export function ScoreBar({ score }) {
  const color = score >= 70 ? '#16a34a' : score >= 40 ? '#ca8a04' : '#94a3b8';
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }} title={`Score ${score} / 100`}>
      <div style={{ width: '48px', height: '6px', backgroundColor: '#e2e8f0', borderRadius: '3px', overflow: 'hidden' }}>
        <div style={{ width: `${score}%`, height: '100%', backgroundColor: color }} />
      </div>
      <span style={{ fontSize: '0.8rem', color: '#475569', minWidth: '24px' }}>{score}</span>
    </div>
  );
}
