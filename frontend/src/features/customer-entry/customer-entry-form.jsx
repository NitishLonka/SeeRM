import React from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';

export default function NewCustomerForm() {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm({
    defaultValues: {
      fullName: '',
      email: '',
      phone: '',
      company: '',
      accountType: 'individual',
    },
  });

  const onSubmit = async (data) => {
    // Simulate an API call
    await new Promise((resolve) => setTimeout(resolve, 1000));
    console.log('New Customer Data:', data);
    alert('Customer created successfully!');
    reset();
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ maxWidth: '400px', margin: '40px auto', padding: '0 16px' }}>
      <Link to="/" style={{ fontSize: '0.875rem', color: '#2563eb', textDecoration: 'none' }}>
        &larr; Back to Dashboard
      </Link>
      <h2>New Customer Registration</h2>

      {/* Full Name */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="fullName">Full Name *</label>
        <input
          id="fullName"
          type="text"
          style={{ display: 'block', width: '100%' }}
          {...register('fullName', {
            required: 'Full name is required',
            minLength: { value: 2, message: 'Name must be at least 2 characters' },
          })}
        />
        {errors.fullName && <span style={{ color: 'red', fontSize: '0.875rem' }}>{errors.fullName.message}</span>}
      </div>

      {/* Email Address */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="email">Email Address *</label>
        <input
          id="email"
          type="email"
          style={{ display: 'block', width: '100%' }}
          {...register('email', {
            required: 'Email is required',
            pattern: {
              value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
              message: 'Please enter a valid email address',
            },
          })}
        />
        {errors.email && <span style={{ color: 'red', fontSize: '0.875rem' }}>{errors.email.message}</span>}
      </div>

      {/* Phone Number */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="phone">Phone Number *</label>
        <input
          id="phone"
          type="tel"
          placeholder="123-456-7890"
          style={{ display: 'block', width: '100%' }}
          {...register('phone', {
            required: 'Phone number is required',
            pattern: {
              value: /^[0-9-+\s()]{7,15}$/,
              message: 'Please enter a valid phone number',
            },
          })}
        />
        {errors.phone && <span style={{ color: 'red', fontSize: '0.875rem' }}>{errors.phone.message}</span>}
      </div>

      {/* Account Type */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="accountType">Account Type</label>
        <select id="accountType" style={{ display: 'block', width: '100%' }} {...register('accountType')}>
          <option value="individual">Individual</option>
          <option value="business">Business</option>
        </select>
      </div>

      {/* Optional Company Name */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="company">Company Name (Optional)</label>
        <input
          id="company"
          type="text"
          style={{ display: 'block', width: '100%' }}
          {...register('company')}
        />
      </div>

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Saving...' : 'Create Customer'}
      </button>
    </form>
  );
}
