import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import { Banner } from '../../components/ui';
import { EDITABLE_LEAD_STATUSES, LEAD_SOURCES } from './lead-constants';

const EMPTY_LEAD = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  company: '',
  jobTitle: '',
  source: 'website',
  status: 'New',
  score: 0,
  estimatedValue: '',
  notes: '',
};

const FORM_FIELDS = Object.keys(EMPTY_LEAD);
const field = { marginBottom: '1rem' };
const input = { display: 'block', width: '100%', boxSizing: 'border-box', padding: '6px 8px' };
const errorText = { color: 'red', fontSize: '0.875rem' };

/** Create (/leads/new) and edit (/leads/:id/edit) form. */
export default function LeadForm() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const [loadError, setLoadError] = useState(null);
  const [loading, setLoading] = useState(isEdit);

  const {
    register,
    handleSubmit,
    reset,
    setError,
    watch,
    formState: { errors, isSubmitting },
  } = useForm({ defaultValues: EMPTY_LEAD });

  useEffect(() => {
    if (!isEdit) return;
    const controller = new AbortController();
    api
      .getLead(id, controller.signal)
      .then((lead) => {
        if (lead.status === 'Converted') {
          setLoadError(`This lead was converted to customer #${lead.convertedCustomerId} and can no longer be edited.`);
        } else {
          reset({
            ...EMPTY_LEAD,
            ...Object.fromEntries(Object.entries(lead).filter(([k, v]) => k in EMPTY_LEAD && v != null)),
          });
        }
        setLoading(false);
      })
      .catch((err) => {
        if (err.name === 'AbortError') return;
        setLoadError(err.message);
        setLoading(false);
      });
    return () => controller.abort();
  }, [id, isEdit, reset]);

  const onSubmit = async (data) => {
    const payload = {
      ...data,
      score: Number(data.score) || 0,
      estimatedValue: data.estimatedValue === '' || data.estimatedValue == null ? null : Number(data.estimatedValue),
    };
    try {
      if (isEdit) await api.updateLead(id, payload);
      else await api.createLead(payload);
      navigate('/leads');
    } catch (err) {
      if (!(err instanceof ApiError)) throw err;
      let mapped = false;
      for (const [name, message] of Object.entries(err.fieldErrors)) {
        if (FORM_FIELDS.includes(name)) {
          setError(name, { type: 'server', message });
          mapped = true;
        }
      }
      if (err.status === 409 && /email/i.test(err.message)) {
        setError('email', { type: 'server', message: 'A lead with this email already exists' });
      } else if (!mapped) {
        setError('root.server', { type: 'server', message: err.message });
      }
    }
  };

  const status = watch('status');
  const phone = watch('phone');

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ maxWidth: '520px', margin: '40px auto', padding: '0 16px', fontFamily: 'sans-serif' }}>
      <Link to="/leads" style={{ fontSize: '0.875rem', color: '#2563eb', textDecoration: 'none' }}>
        &larr; Back to Leads
      </Link>
      <h2>{isEdit ? 'Edit Lead' : 'New Lead'}</h2>

      {loading && <p style={{ color: '#64748b' }}>Loading lead…</p>}
      {loadError && <Banner kind="error">{loadError}</Banner>}

      {!loading && !loadError && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0 16px' }}>
            <div style={field}>
              <label htmlFor="firstName">First Name *</label>
              <input id="firstName" style={input} {...register('firstName', { required: 'First name is required', maxLength: { value: 80, message: 'Max 80 characters' } })} />
              {errors.firstName && <span style={errorText}>{errors.firstName.message}</span>}
            </div>
            <div style={field}>
              <label htmlFor="lastName">Last Name *</label>
              <input id="lastName" style={input} {...register('lastName', { required: 'Last name is required', maxLength: { value: 80, message: 'Max 80 characters' } })} />
              {errors.lastName && <span style={errorText}>{errors.lastName.message}</span>}
            </div>
          </div>

          <div style={field}>
            <label htmlFor="email">Email Address *</label>
            <input
              id="email"
              type="email"
              style={input}
              {...register('email', {
                required: 'Email is required',
                pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Please enter a valid email address' },
              })}
            />
            {errors.email && <span style={errorText}>{errors.email.message}</span>}
          </div>

          <div style={field}>
            <label htmlFor="phone">Phone Number</label>
            <input
              id="phone"
              type="tel"
              placeholder="123-456-7890"
              style={input}
              {...register('phone', {
                pattern: { value: /^[0-9-+\s()]{7,15}$/, message: 'Please enter a valid phone number' },
              })}
            />
            {errors.phone && <span style={errorText}>{errors.phone.message}</span>}
            {status === 'Qualified' && !phone && !errors.phone && (
              <span style={{ fontSize: '0.8rem', color: '#92400e' }}>A phone number is required before this lead can be converted.</span>
            )}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0 16px' }}>
            <div style={field}>
              <label htmlFor="company">Company</label>
              <input id="company" style={input} {...register('company')} />
            </div>
            <div style={field}>
              <label htmlFor="jobTitle">Job Title</label>
              <input id="jobTitle" style={input} {...register('jobTitle')} />
            </div>
            <div style={field}>
              <label htmlFor="source">Source</label>
              <select id="source" style={input} {...register('source')}>
                {LEAD_SOURCES.map((s) => (
                  <option key={s.value} value={s.value}>
                    {s.label}
                  </option>
                ))}
              </select>
            </div>
            <div style={field}>
              <label htmlFor="status">Status</label>
              <select id="status" style={input} {...register('status')}>
                {EDITABLE_LEAD_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
            <div style={field}>
              <label htmlFor="score">Score (0–100)</label>
              <input
                id="score"
                type="number"
                min="0"
                max="100"
                style={input}
                {...register('score', {
                  min: { value: 0, message: 'Score must be between 0 and 100' },
                  max: { value: 100, message: 'Score must be between 0 and 100' },
                })}
              />
              {errors.score && <span style={errorText}>{errors.score.message}</span>}
            </div>
            <div style={field}>
              <label htmlFor="estimatedValue">Estimated Value ($)</label>
              <input
                id="estimatedValue"
                type="number"
                min="0"
                step="0.01"
                style={input}
                {...register('estimatedValue', { min: { value: 0, message: 'Estimated value cannot be negative' } })}
              />
              {errors.estimatedValue && <span style={errorText}>{errors.estimatedValue.message}</span>}
            </div>
          </div>

          <div style={field}>
            <label htmlFor="notes">Notes</label>
            <textarea id="notes" rows="3" style={input} {...register('notes', { maxLength: { value: 2000, message: 'Max 2000 characters' } })} />
            {errors.notes && <span style={errorText}>{errors.notes.message}</span>}
          </div>

          {errors.root?.server && (
            <p role="alert" style={errorText}>
              {errors.root.server.message}
            </p>
          )}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : isEdit ? 'Save Changes' : 'Create Lead'}
          </button>
        </>
      )}
    </form>
  );
}
