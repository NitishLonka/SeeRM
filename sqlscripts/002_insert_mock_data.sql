-- SeeRM mock data. Run after 001_create_tables.sql.
-- Timestamps are relative to now() so "Last Contact" and due dates stay realistic.
-- The first four customers match the hard-coded rows in the dashboard UI
-- (deal value $14,500 / $8,200 / $32,000 / $4,100; last contact 2h / 1d / 3d / 2w),
-- and tasks are seeded as 18 of 22 completed with 3 follow-ups due this week.
-- All emails use the reserved .example TLD.

BEGIN;

TRUNCATE interactions, tasks, deals, customers RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------------
-- customers
-- ---------------------------------------------------------------------------
INSERT INTO customers (id, full_name, email, phone, account_type, company_name, status, created_at) VALUES
 ( 1, 'Sarah Jenkins',    'sarah.jenkins@apexlogistics.example',      '555-201-1001', 'business',   'Apex Logistics',     'Active',   now() - interval '180 days'),
 ( 2, 'Marcus Vance',     'marcus.vance@brightline.example',          '555-201-1002', 'business',   'Brightline Tech',    'Lead',     now() - interval '12 days'),
 ( 3, 'Elena Rostova',    'elena.rostova@horizonretail.example',      '555-201-1003', 'business',   'Horizon Retail',     'Active',   now() - interval '150 days'),
 ( 4, 'David Kim',        'david.kim@kestrelmedia.example',           '555-201-1004', 'business',   'Kestrel Media',      'Inactive', now() - interval '240 days'),
 ( 5, 'Priya Raman',      'priya.raman@northwindfoods.example',       '555-201-1005', 'business',   'Northwind Foods',    'Active',   now() - interval '95 days'),
 ( 6, 'Tom Okafor',       'tom.okafor@mail.example',                  '555-201-1006', 'individual', NULL,                 'Lead',     now() - interval '6 days'),
 ( 7, 'Grace Liu',        'grace.liu@summitdental.example',           '555-201-1007', 'business',   'Summit Dental',      'Active',   now() - interval '70 days'),
 ( 8, 'Jonas Berg',       'jonas.berg@fjordanalytics.example',        '555-201-1008', 'business',   'Fjord Analytics',    'Lead',     now() - interval '25 days'),
 ( 9, 'Amara Nwosu',      'amara.nwosu@mail.example',                 '555-201-1009', 'individual', NULL,                 'Active',   now() - interval '20 days'),
 (10, 'Luis Ortega',      'luis.ortega@cascadebuilders.example',      '555-201-1010', 'business',   'Cascade Builders',   'Active',   now() - interval '130 days'),
 (11, 'Hannah Schmidt',   'hannah.schmidt@lumenhealth.example',       '555-201-1011', 'business',   'Lumen Health',       'Lead',     now() - interval '15 days'),
 (12, 'Ravi Patel',       'ravi.patel@mail.example',                  '555-201-1012', 'individual', NULL,                 'Inactive', now() - interval '110 days'),
 (13, 'Chloe Martin',     'chloe.martin@verdeorganics.example',       '555-201-1013', 'business',   'Verde Organics',     'Active',   now() - interval '60 days'),
 (14, 'Kenji Watanabe',   'kenji.watanabe@orbitrobotics.example',     '555-201-1014', 'business',   'Orbit Robotics',     'Lead',     now() - interval '9 days'),
 (15, 'Fatima Al-Sayed',  'fatima.alsayed@crescentfinance.example',   '555-201-1015', 'business',   'Crescent Finance',   'Active',   now() - interval '100 days'),
 (16, 'Noah Fischer',     'noah.fischer@mail.example',                '555-201-1016', 'individual', NULL,                 'Lead',     now() - interval '1 day'),
 (17, 'Isabella Rossi',   'isabella.rossi@bellavistahotels.example',  '555-201-1017', 'business',   'Bella Vista Hotels', 'Active',   now() - interval '85 days'),
 (18, 'Owen Price',       'owen.price@ironcladsecurity.example',      '555-201-1018', 'business',   'Ironclad Security',  'Inactive', now() - interval '200 days'),
 (19, 'Mei Chen',         'mei.chen@jadeinteriors.example',           '555-201-1019', 'business',   'Jade Interiors',     'Lead',     now() - interval '18 days'),
 (20, 'Samuel Adeyemi',   'samuel.adeyemi@mail.example',              '555-201-1020', 'individual', NULL,                 'Active',   now() - interval '8 days');

-- ---------------------------------------------------------------------------
-- deals  (won/lost deals have closed_at; open deals have an expected close date)
-- ---------------------------------------------------------------------------
INSERT INTO deals (id, customer_id, title, value, stage, expected_close_date, closed_at, created_at) VALUES
 ( 1,  1, 'Fleet tracking rollout',        9500.00, 'won',           current_date - 40, now() - interval '40 days',  now() - interval '170 days'),
 ( 2,  1, 'Warehouse analytics add-on',    5000.00, 'proposal',      current_date + 20, NULL,                        now() - interval '15 days'),
 ( 3,  2, 'Starter platform license',      8200.00, 'qualification', current_date + 35, NULL,                        now() - interval '10 days'),
 ( 4,  3, 'POS integration',              20000.00, 'won',           current_date - 60, now() - interval '60 days',  now() - interval '140 days'),
 ( 5,  3, 'Loyalty program module',       12000.00, 'negotiation',   current_date + 10, NULL,                        now() - interval '30 days'),
 ( 6,  3, 'In-store kiosk pilot',          6000.00, 'lost',          current_date - 15, now() - interval '15 days',  now() - interval '50 days'),
 ( 7,  4, 'Ad campaign CRM seats',         4100.00, 'won',           current_date - 120, now() - interval '120 days', now() - interval '230 days'),
 ( 8,  5, 'Supply chain dashboard',       18000.00, 'negotiation',   current_date + 14, NULL,                        now() - interval '40 days'),
 ( 9,  5, 'Annual support renewal',        3500.00, 'won',           current_date - 25, now() - interval '25 days',  now() - interval '45 days'),
 (10,  6, 'Personal plan',                  900.00, 'prospecting',   current_date + 30, NULL,                        now() - interval '5 days'),
 (11,  7, 'Patient outreach suite',        7200.00, 'won',           current_date - 50, now() - interval '50 days',  now() - interval '65 days'),
 (12,  7, 'Multi-clinic expansion',       11000.00, 'proposal',      current_date + 25, NULL,                        now() - interval '20 days'),
 (13,  8, 'Data pipeline consulting',     15000.00, 'qualification', current_date + 45, NULL,                        now() - interval '22 days'),
 (14,  9, 'Premium individual plan',       1200.00, 'won',           current_date - 10, now() - interval '10 days',  now() - interval '18 days'),
 (15, 10, 'Project bidding module',       22500.00, 'won',           current_date - 90, now() - interval '90 days',  now() - interval '125 days'),
 (16, 10, 'Subcontractor portal',          9800.00, 'proposal',      current_date + 18, NULL,                        now() - interval '28 days'),
 (17, 11, 'Clinic CRM pilot',              6400.00, 'prospecting',   current_date + 40, NULL,                        now() - interval '14 days'),
 (18, 12, 'Personal plan',                  600.00, 'lost',          current_date - 70, now() - interval '70 days',  now() - interval '105 days'),
 (19, 13, 'E-commerce sync',               8700.00, 'won',           current_date - 30, now() - interval '30 days',  now() - interval '55 days'),
 (20, 13, 'Wholesale channel setup',       5400.00, 'negotiation',   current_date + 7,  NULL,                        now() - interval '12 days'),
 (21, 14, 'Robotics fleet telemetry',     27000.00, 'qualification', current_date + 60, NULL,                        now() - interval '8 days'),
 (22, 15, 'Compliance reporting',         16500.00, 'won',           current_date - 75, now() - interval '75 days',  now() - interval '98 days'),
 (23, 15, 'Advisor portal',               13200.00, 'proposal',      current_date + 21, NULL,                        now() - interval '20 days'),
 (24, 16, 'Freelancer plan',                750.00, 'prospecting',   current_date + 15, NULL,                        now() - interval '1 day'),
 (25, 17, 'Guest experience platform',    19800.00, 'won',           current_date - 45, now() - interval '45 days',  now() - interval '80 days'),
 (26, 17, 'Second property rollout',       9900.00, 'negotiation',   current_date + 12, NULL,                        now() - interval '18 days'),
 (27, 18, 'Security ops seats',            5600.00, 'lost',          current_date - 100, now() - interval '100 days', now() - interval '190 days'),
 (28, 19, 'Design studio CRM',             4300.00, 'prospecting',   current_date + 28, NULL,                        now() - interval '16 days'),
 (29, 20, 'Premium individual plan',       1200.00, 'won',           current_date - 5,  now() - interval '5 days',   now() - interval '7 days'),
 (30,  8, 'Training workshop',             2500.00, 'lost',          current_date - 20, now() - interval '20 days',  now() - interval '24 days');

-- ---------------------------------------------------------------------------
-- tasks  (22 total, 18 completed; open tasks 4, 6, 12 are due within 7 days)
-- ---------------------------------------------------------------------------
INSERT INTO tasks (id, customer_id, deal_id, title, due_date, status, completed_at, created_at) VALUES
 ( 1,  1,    2, 'Send warehouse add-on proposal',          current_date - 10, 'completed', now() - interval '11 days', now() - interval '15 days'),
 ( 2,  1, NULL, 'Quarterly business review',               current_date - 3,  'completed', now() - interval '3 days',  now() - interval '20 days'),
 ( 3,  2,    3, 'Discovery call with Marcus',              current_date - 5,  'completed', now() - interval '5 days',  now() - interval '10 days'),
 ( 4,  2,    3, 'Follow up on pricing questions',          current_date + 1,  'open',      NULL,                       now() - interval '1 day'),
 ( 5,  3,    5, 'Share loyalty module demo recording',     current_date - 2,  'completed', now() - interval '2 days',  now() - interval '9 days'),
 ( 6,  3,    5, 'Negotiate contract terms',                current_date + 2,  'open',      NULL,                       now() - interval '3 days'),
 ( 7,  4, NULL, 'Re-engagement email',                     current_date - 14, 'completed', now() - interval '14 days', now() - interval '21 days'),
 ( 8,  5,    8, 'Prepare supply chain dashboard mockups',  current_date - 6,  'completed', now() - interval '7 days',  now() - interval '20 days'),
 ( 9,  5,    9, 'Process renewal invoice',                 current_date - 25, 'completed', now() - interval '25 days', now() - interval '30 days'),
 (10,  6,   10, 'Intro call',                              current_date - 4,  'completed', now() - interval '4 days',  now() - interval '5 days'),
 (11,  7,   12, 'Site visit to second clinic',             current_date - 8,  'completed', now() - interval '8 days',  now() - interval '18 days'),
 (12,  8,   13, 'Send data pipeline scope document',       current_date + 3,  'open',      NULL,                       now() - interval '1 day'),
 (13,  9,   14, 'Onboarding session',                      current_date - 9,  'completed', now() - interval '9 days',  now() - interval '12 days'),
 (14, 10,   16, 'Demo subcontractor portal',               current_date - 1,  'completed', now() - interval '1 day',   now() - interval '10 days'),
 (15, 11,   17, 'Qualify budget and timeline',             current_date - 12, 'completed', now() - interval '12 days', now() - interval '14 days'),
 (16, 13,   20, 'Draft wholesale setup SOW',               current_date - 3,  'completed', now() - interval '4 days',  now() - interval '10 days'),
 (17, 14,   21, 'Technical deep-dive with engineering',    current_date + 10, 'open',      NULL,                       now() - interval '2 days'),
 (18, 15,   23, 'Advisor portal requirements workshop',    current_date - 6,  'completed', now() - interval '6 days',  now() - interval '15 days'),
 (19, 17,   26, 'Send revised pricing',                    current_date - 2,  'completed', now() - interval '2 days',  now() - interval '8 days'),
 (20, 18, NULL, 'Exit interview call',                     current_date - 30, 'completed', now() - interval '30 days', now() - interval '35 days'),
 (21, 19,   28, 'Share portfolio case studies',            current_date - 7,  'completed', now() - interval '7 days',  now() - interval '12 days'),
 (22, 20,   29, 'Welcome kit and onboarding',              current_date - 5,  'completed', now() - interval '5 days',  now() - interval '7 days');

-- ---------------------------------------------------------------------------
-- interactions  (latest per customer drives "Last Contact")
-- ---------------------------------------------------------------------------
INSERT INTO interactions (customer_id, type, summary, occurred_at) VALUES
 ( 1, 'meeting', 'Quarterly business review; discussed warehouse analytics add-on', now() - interval '2 hours'),
 ( 1, 'email',   'Sent warehouse add-on proposal',                                  now() - interval '11 days'),
 ( 2, 'call',    'Answered pricing questions on starter license',                   now() - interval '1 day'),
 ( 2, 'call',    'Discovery call',                                                  now() - interval '5 days'),
 ( 3, 'email',   'Shared loyalty module demo recording',                            now() - interval '3 days'),
 ( 3, 'meeting', 'Contract terms kickoff',                                          now() - interval '12 days'),
 ( 4, 'email',   'Re-engagement email; no response yet',                            now() - interval '14 days'),
 ( 4, 'call',    'Account review',                                                  now() - interval '60 days'),
 ( 5, 'meeting', 'Reviewed supply chain dashboard mockups',                         now() - interval '6 hours'),
 ( 5, 'email',   'Renewal invoice sent',                                            now() - interval '25 days'),
 ( 6, 'call',    'Intro call; interested in personal plan',                         now() - interval '4 days'),
 ( 7, 'email',   'Followed up on expansion proposal',                               now() - interval '2 days'),
 ( 7, 'meeting', 'Site visit to second clinic',                                     now() - interval '8 days'),
 ( 8, 'email',   'Questions on data pipeline scope document',                       now() - interval '20 hours'),
 ( 8, 'note',    'Training workshop deal lost to budget freeze',                    now() - interval '20 days'),
 ( 9, 'call',    'Onboarding session completed',                                    now() - interval '9 days'),
 (10, 'meeting', 'Subcontractor portal demo',                                       now() - interval '1 day'),
 (11, 'call',    'Budget and timeline qualification',                               now() - interval '12 days'),
 (12, 'note',    'Chose a competitor; deal closed lost',                            now() - interval '70 days'),
 (13, 'email',   'Sent wholesale channel SOW draft',                                now() - interval '4 days'),
 (14, 'call',    'Intro with engineering lead',                                     now() - interval '2 days'),
 (15, 'meeting', 'Advisor portal requirements workshop',                            now() - interval '6 days'),
 (16, 'email',   'Inbound inquiry about freelancer plan',                           now() - interval '3 hours'),
 (17, 'email',   'Sent revised pricing for second property',                        now() - interval '2 days'),
 (18, 'call',    'Exit interview call',                                             now() - interval '30 days'),
 (19, 'email',   'Shared portfolio case studies',                                   now() - interval '7 days'),
 (20, 'call',    'Welcome and onboarding call',                                     now() - interval '5 days');

-- Move identity sequences past the explicit IDs so app inserts don't collide.
SELECT setval(pg_get_serial_sequence('customers', 'id'), (SELECT MAX(id) FROM customers));
SELECT setval(pg_get_serial_sequence('deals', 'id'),     (SELECT MAX(id) FROM deals));
SELECT setval(pg_get_serial_sequence('tasks', 'id'),     (SELECT MAX(id) FROM tasks));

COMMIT;
