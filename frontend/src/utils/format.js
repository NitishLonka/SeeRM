const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 0,
});

/** 14500 -> "$14,500" */
export function formatCurrency(value) {
  return currencyFormatter.format(Number(value) || 0);
}

/** ISO timestamp -> "2 hours ago", "1 day ago", "2 weeks ago". Null -> "Never". */
export function formatRelativeTime(isoString, now = Date.now()) {
  if (!isoString) return 'Never';
  const seconds = Math.max(0, Math.round((now - new Date(isoString).getTime()) / 1000));

  const units = [
    ['year', 365 * 24 * 3600],
    ['month', 30 * 24 * 3600],
    ['week', 7 * 24 * 3600],
    ['day', 24 * 3600],
    ['hour', 3600],
    ['minute', 60],
  ];
  for (const [unit, size] of units) {
    const amount = Math.floor(seconds / size);
    if (amount >= 1) return `${amount} ${unit}${amount === 1 ? '' : 's'} ago`;
  }
  return 'Just now';
}
