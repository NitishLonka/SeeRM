-- Leads mock data. Run after 001_create_tables.sql.
-- 25 leads: 7 New, 6 Contacted, 6 Qualified, 3 Unqualified, 3 Converted.
-- Timestamps are relative to now(); several leads are from the last 7 days ("new this week").
-- Converted leads point at existing customers in the Customer DB (ids 6, 16, 20; same emails).
-- Demo cases for the Convert flow (phase 3):
--   * lead 17 (Mei Chen) is Qualified and shares an email with Customer 19 -> conversion links to the existing customer
--   * lead 18 (Arjun Mehta) is Qualified but has no phone -> conversion is rejected (Customer requires a phone)
-- All emails use the reserved .example TLD.

BEGIN;

TRUNCATE leads RESTART IDENTITY;

INSERT INTO leads (id, first_name, last_name, email, phone, company_name, job_title, source, status, score, estimated_value, notes, converted_customer_id, converted_at, created_at) VALUES
 -- New
 ( 1, 'Olivia',  'Bennett',   'olivia.bennett@tidewaterfreight.example',  '555-301-2001', 'Tidewater Freight',    'Operations Manager',   'website',   'New',          35,  12000.00, 'Downloaded the logistics whitepaper.',              NULL, NULL, now() - interval '1 day'),
 ( 2, 'Ethan',   'Cole',      'ethan.cole@peakfitness.example',           '555-301-2002', 'Peak Fitness Studios', 'Owner',                'social',    'New',          20,   3500.00, NULL,                                                NULL, NULL, now() - interval '2 days'),
 ( 3, 'Sofia',   'Marquez',   'sofia.marquez@mail.example',               '555-301-2003', NULL,                   NULL,                   'website',   'New',          15,    900.00, 'Asked about individual plan pricing via chat.',     NULL, NULL, now() - interval '3 days'),
 ( 4, 'Liam',    'O''Connor', 'liam.oconnor@greenleafcafe.example',       NULL,           'Greenleaf Cafe',       'Co-founder',           'event',     'New',          25,   2400.00, 'Met at the regional small business expo.',          NULL, NULL, now() - interval '5 days'),
 ( 5, 'Ava',     'Thompson',  'ava.thompson@brightpathedu.example',       '555-301-2005', 'BrightPath Education', 'Director of Admissions','referral', 'New',          40,  18000.00, 'Referred by Grace Liu (Summit Dental).',            NULL, NULL, now() - interval '6 days'),
 ( 6, 'Lucas',   'Wright',    'lucas.wright@stonebridgelaw.example',      '555-301-2006', 'Stonebridge Law',      'Office Manager',       'cold_call', 'New',          10,   6000.00, NULL,                                                NULL, NULL, now() - interval '9 days'),
 ( 7, 'Zara',    'Hussain',   'zara.hussain@novapharma.example',          '555-301-2007', 'Nova Pharma',          'Procurement Lead',     'website',   'New',          30,  25000.00, 'Requested a demo for 40 seats.',                    NULL, NULL, now() - interval '12 days'),
 -- Contacted
 ( 8, 'Daniel',  'Park',      'daniel.park@urbannest.example',            '555-301-2008', 'UrbanNest Realty',     'Broker',               'referral',  'Contacted',    50,   9500.00, 'Intro call done; wants pricing for 12 agents.',     NULL, NULL, now() - interval '14 days'),
 ( 9, 'Emma',    'Schultz',   'emma.schultz@alpinegear.example',          '555-301-2009', 'Alpine Gear Co',       'E-commerce Manager',   'website',   'Contacted',    45,  14000.00, NULL,                                                NULL, NULL, now() - interval '16 days'),
 (10, 'Noel',    'Garcia',    'noel.garcia@mail.example',                 '555-301-2010', NULL,                   NULL,                   'social',    'Contacted',    30,    750.00, 'Freelance designer; follow up next month.',         NULL, NULL, now() - interval '18 days'),
 (11, 'Hiro',    'Tanaka',    'hiro.tanaka@sakuralogistics.example',      '555-301-2011', 'Sakura Logistics',     'IT Director',          'event',     'Contacted',    55,  30000.00, 'Needs SSO and audit log; sent security overview.', NULL, NULL, now() - interval '20 days'),
 (12, 'Maya',    'Robinson',  'maya.robinson@harborvet.example',          '555-301-2012', 'Harbor Veterinary',    'Practice Manager',     'cold_call', 'Contacted',    35,   4800.00, NULL,                                                NULL, NULL, now() - interval '22 days'),
 (13, 'Felix',   'Wagner',    'felix.wagner@quantumbuild.example',        '555-301-2013', 'Quantum Build',        'Project Manager',      'website',   'Contacted',    40,  11000.00, 'Evaluating two other CRMs.',                        NULL, NULL, now() - interval '25 days'),
 -- Qualified
 (14, 'Nora',    'Lindqvist', 'nora.lindqvist@polarisenergy.example',     '555-301-2014', 'Polaris Energy',       'Head of Sales',        'referral',  'Qualified',    80,  42000.00, 'Budget approved for Q1; decision maker confirmed.', NULL, NULL, now() - interval '28 days'),
 (15, 'Caleb',   'Morris',    'caleb.morris@redwoodclinics.example',      '555-301-2015', 'Redwood Clinics',      'COO',                  'event',     'Qualified',    75,  26000.00, 'Pilot at two clinics first.',                       NULL, NULL, now() - interval '30 days'),
 (16, 'Leah',    'Kim',       'leah.kim@mail.example',                    '555-301-2016', NULL,                   NULL,                   'website',   'Qualified',    70,   1200.00, 'Ready to buy the premium individual plan.',         NULL, NULL, now() - interval '8 days'),
 (17, 'Mei',     'Chen',      'mei.chen@jadeinteriors.example',           '555-201-1019', 'Jade Interiors',       'Principal Designer',   'referral',  'Qualified',    85,   6500.00, 'Already a customer; interested in add-on seats.',   NULL, NULL, now() - interval '4 days'),
 (18, 'Arjun',   'Mehta',     'arjun.mehta@vertexai.example',             NULL,           'Vertex AI Labs',       'CTO',                  'social',    'Qualified',    78,  35000.00, 'No phone number on file yet.',                      NULL, NULL, now() - interval '10 days'),
 (19, 'Grace',   'Okafor',    'grace.okafor@sunrisebakery.example',       '555-301-2019', 'Sunrise Bakery',       'Owner',                'cold_call', 'Qualified',    65,   3200.00, NULL,                                                NULL, NULL, now() - interval '15 days'),
 -- Unqualified
 (20, 'Victor',  'Reyes',     'victor.reyes@mail.example',                '555-301-2020', NULL,                   NULL,                   'social',    'Unqualified',   5,      NULL, 'Student project; no budget.',                       NULL, NULL, now() - interval '35 days'),
 (21, 'Isla',    'Murphy',    'isla.murphy@coastalprint.example',         '555-301-2021', 'Coastal Print',        'Owner',                'cold_call', 'Unqualified',  10,   1500.00, 'Happy with current spreadsheet setup.',             NULL, NULL, now() - interval '40 days'),
 (22, 'Omar',    'Farouk',    'omar.farouk@desertlogix.example',          '555-301-2022', 'Desert Logix',         'Analyst',              'website',   'Unqualified',  15,   8000.00, 'Not a decision maker; company locked into vendor.', NULL, NULL, now() - interval '45 days'),
 -- Converted (linked to existing customers in the Customer DB)
 (23, 'Tom',     'Okafor',    'tom.okafor@mail.example',                  '555-201-1006', NULL,                   NULL,                   'website',   'Converted',    70,    900.00, 'Converted to customer.',                            6,    now() - interval '6 days',  now() - interval '20 days'),
 (24, 'Noah',    'Fischer',   'noah.fischer@mail.example',                '555-201-1016', NULL,                   NULL,                   'social',    'Converted',    60,    750.00, 'Converted to customer.',                            16,   now() - interval '1 day',   now() - interval '10 days'),
 (25, 'Samuel',  'Adeyemi',   'samuel.adeyemi@mail.example',              '555-201-1020', NULL,                   NULL,                   'referral',  'Converted',    75,   1200.00, 'Converted to customer.',                            20,   now() - interval '8 days',  now() - interval '25 days');

SELECT setval(pg_get_serial_sequence('leads', 'id'), (SELECT MAX(id) FROM leads));

COMMIT;
