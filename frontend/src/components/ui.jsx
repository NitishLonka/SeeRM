// Small shared UI pieces used by the Dashboard and Leads screens.

export const pageStyle = { fontFamily: 'sans-serif', backgroundColor: '#f8fafc', minHeight: '100vh', padding: '24px' };
export const cardStyle = { backgroundColor: '#fff', borderRadius: '8px', padding: '20px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' };
export const primaryButtonStyle = {
  backgroundColor: '#2563eb',
  color: '#fff',
  textDecoration: 'none',
  padding: '10px 16px',
  borderRadius: '6px',
  fontWeight: '600',
  border: 'none',
  cursor: 'pointer',
};
export const secondaryLinkStyle = {
  color: '#2563eb',
  textDecoration: 'none',
  padding: '10px 16px',
  borderRadius: '6px',
  fontWeight: '600',
  border: '1px solid #bfdbfe',
  backgroundColor: '#fff',
};

/** KPI card. `trend` is optional. */
export function StatCard({ title, value, trend }) {
  return (
    <div style={{ backgroundColor: '#fff', padding: '16px', borderRadius: '8px', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
      <p style={{ margin: 0, fontSize: '0.875rem', color: '#64748b' }}>{title}</p>
      <h3 style={{ margin: '8px 0 4px', fontSize: '1.5rem', color: '#0f172a' }}>{value}</h3>
      {trend && <span style={{ fontSize: '0.75rem', color: '#16a34a' }}>{trend}</span>}
    </div>
  );
}

/** Full-width table row for loading / empty / error messages. */
export function MessageRow({ children, colSpan = 5, color = '#64748b' }) {
  return (
    <tr>
      <td colSpan={colSpan} style={{ padding: '20px', textAlign: 'center', color }}>
        {children}
      </td>
    </tr>
  );
}

const bannerColors = {
  error: { bg: '#fef2f2', text: '#b91c1c', border: '#fecaca' },
  success: { bg: '#f0fdf4', text: '#166534', border: '#bbf7d0' },
  warning: { bg: '#fffbeb', text: '#92400e', border: '#fde68a' },
};

/** Coloured message box. `onDismiss` adds a close button. */
export function Banner({ kind = 'error', children, onDismiss }) {
  const c = bannerColors[kind];
  return (
    <div
      role={kind === 'error' ? 'alert' : 'status'}
      style={{
        backgroundColor: c.bg,
        color: c.text,
        border: `1px solid ${c.border}`,
        borderRadius: '6px',
        padding: '10px 14px',
        marginBottom: '16px',
        fontSize: '0.875rem',
        display: 'flex',
        justifyContent: 'space-between',
        gap: '12px',
        alignItems: 'flex-start',
      }}
    >
      <div>{children}</div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss"
          style={{ background: 'none', border: 'none', color: c.text, cursor: 'pointer', fontSize: '1rem', lineHeight: 1 }}
        >
          ×
        </button>
      )}
    </div>
  );
}

export function ErrorBanner({ message }) {
  return <Banner kind="error">{message}</Banner>;
}
