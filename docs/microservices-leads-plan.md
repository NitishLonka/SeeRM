# Plan: Leads Microservice, Customer DB, and API Gateway

Status: **complete (all 6 phases done, 2026-11-25)** · Decisions agreed 2026-11-25

| Decision | Choice |
|---|---|
| New module | **Leads** |
| "Customer DB not yet created" | New **Customer service DB**, carved out of the current monolith |
| Leads → Customer integration | **Synchronous REST** |
| Scope of this iteration | **New service + API gateway** |

## 1. Target architecture

```
React (localhost:3000)
   │  all calls go to one base URL
   ▼
API Gateway – YARP (localhost:5000)
   ├── /api/leads/**                         ─▶ Leads service    (localhost:5080) ─▶ postgres-leads    (DB: leads)
   └── /api/customers/** /api/deals/** ...   ─▶ Customer service (localhost:5067) ─▶ postgres-customer (DB: customer)

Leads service ──sync REST (direct, not via the gateway)──▶ Customer service   (on "Convert lead")
```

- **Every service owns its database:** a separate Postgres pod, volume and credentials. No cross-database queries; services talk only over HTTP.
- **Customer service** is the existing `backend/seerm-dotnet` API, repointed to the new `customer` database.
- **Leads service** is a new .NET 10 API with the same structure (EF Core, Swagger, ProblemDetails errors).
- **Gateway:** React talks only to the gateway, which also handles CORS. Services no longer need CORS.

## 2. Leads module

### Main screen (`/leads`)
- **KPI cards:** new leads this week, qualified leads, conversion rate, estimated value of open leads.
- **Status tabs:** All / New / Contacted / Qualified / Unqualified / Converted.
- **Search** on name, company or email; filter by source.
- **Table:** name, company, source, status, score, estimated value, created.
- **Row actions:** edit, change status, delete. **Convert to Customer** is enabled only for Qualified leads.
- **"+ New Lead" form**, and a "Leads" link in the dashboard header.

### Data model (`leads` table in the `leads` database)

| Column | Notes |
|---|---|
| id, first_name, last_name, email, phone, company_name, job_title | phone optional here, but required to convert because Customer requires it |
| source | website \| referral \| event \| cold_call \| social \| other |
| status | New \| Contacted \| Qualified \| Unqualified \| Converted |
| score (0–100), estimated_value, notes | |
| converted_customer_id, converted_at | ID in the Customer service. **Not a foreign key**: that table lives in another service's database |
| created_at, updated_at | `updated_at` maintained by trigger |

### API
- `GET /api/leads` (search, status, source, paging)
- `GET /api/leads/summary`
- `GET/POST/PUT/DELETE /api/leads/{id}`
- `POST /api/leads/{id}/convert`

## 3. Convert flow (Leads → Customer DB)

`POST /api/leads/{id}/convert`:
1. Lead must be `Qualified` and have a phone number → otherwise `400`.
2. `GET /api/customers?email=…` on the Customer service:
   - **Customer exists:** link the lead to it (no duplicate).
   - **Not found:** `POST /api/customers`. Full name = first + last name; account type `business` if there's a company; status `Active`.
3. If the lead has an estimated value, also `POST /api/customers/{id}/deals`.
4. Mark the lead `Converted` and store `converted_customer_id`.

Reliability (as implemented in `Integration/CustomerServiceClient.cs` and `Services/LeadConversionService.cs`):
- **Customer service down:** 1 s connect timeout, 3 s per attempt, 8 s total, 1 retry for GETs only (`Microsoft.Extensions.Http.Resilience`). If it still fails, return `503` (in about 2–3 s) and leave the lead unchanged so the user can retry. Leads `/health` reports **Degraded**, and lead CRUD keeps working.
- **Idempotent:** the email lookup makes retries safe, even if step 4 failed after the customer was created. A `409` from the Customer service on create (concurrent conversion) falls back to the lookup.
- **Deal de-duplication:** the deal title is deterministic (`"<company or name> (lead #<id>)"`), and an existing deal with that title is reused. If deal creation fails, the conversion still succeeds and returns a warning.
- **Existing customers are not modified** when a lead is linked to them (e.g. their status is left as is).
- **Validation errors** from the Customer service are passed through to the UI.
- **Trade-off of sync REST:** conversion only works while the Customer service is up. Switching to events later wouldn't change the convert endpoint's shape.

## 4. Phases

| # | Work | Done when |
|---|---|---|
| 1 ✅ | **Customer DB:** `postgres-customer` pod; existing scripts moved to `sqlscripts/customer/` and run there; Customer service repointed; `?email=` lookup and `/health` added | Current dashboard and form work unchanged against the new DB |
| 2 ✅ | **Leads service and DB:** `postgres-leads` pod; `sqlscripts/leads/` schema plus ~25 mock leads; EF Core service; Swagger | Leads CRUD works in Swagger |
| 3 ✅ | **Convert:** typed HttpClient, resilience, link-or-create, optional deal | Converted lead appears in the customer directory; retry makes no duplicate; Customer service stopped → 503 |
| 4 ✅ | **Gateway:** `backend/gateway` (YARP) routes, CORS, health | Everything reachable via `localhost:5000` |
| 5 ✅ | **Frontend:** `features/leads/` screen and form, route and nav link, tests (API base URL already switched to the gateway in phase 4) | Leads screen works end to end in the browser |
| 6 ✅ | **Docs:** architecture and Leads design doc; old `seerm` DB marked for retirement | Docs match the code |

### Repository layout

```
backend/
  seerm-dotnet/          Customer service (existing; rename later)
  services/leads/        Leads service (new)
  gateway/               YARP API gateway (new)
k8s/
  postgres/              old monolith DB (retire after phase 1 is verified)
  postgres-customer/     Customer DB (new)
  postgres-leads/        Leads DB (new)
sqlscripts/
  customer/              Customer schema + mock data
  leads/                 Leads schema + mock data
```

### Ports

| Component | Port |
|---|---|
| API gateway | 5000 |
| Customer service | 5067 |
| Leads service | 5080 |
| postgres (old `seerm`) | 30432 |
| postgres-customer | 30433 |
| postgres-leads | 30434 |
| React dev server | 3000 |

## 5. Gateway behaviour (as implemented in `backend/gateway/SeeRM.Gateway`)

| Path | Routed to |
|---|---|
| `/api/leads/**` | Leads service |
| `/api/customers/**`, `/api/deals/**`, `/api/tasks/**`, `/api/interactions/**`, `/api/dashboard/**` | Customer service |
| `/docs/customer/swagger.json`, `/docs/leads/swagger.json` | each service's OpenAPI document |
| `/swagger` | combined Swagger UI (service picker; "Try it out" goes through the gateway) |
| `/health` | gateway + each service, as JSON; `Degraded` if a service is down |
| anything else | 404 |

- **CORS** is handled only by the gateway; the services no longer enable it (duplicate headers would be rejected by browsers). React's `REACT_APP_API_BASE_URL` points to `http://localhost:5000`.
- **Original host is forwarded**, so `Location` headers point at the gateway (e.g. `http://localhost:5000/api/leads/26`).
- **Service down:** 1 s connect timeout; YARP active health checks every 5 s on `/health`. A service known to be down gets an immediate `503` with ProblemDetails naming it ("The leads service is unavailable right now…"). Panic mode is off (`AvailableDestinationsPolicy: HealthyAndUnknown`). Routing resumes automatically when the service is back.
- The Leads → Customer call during conversion goes **directly** to the Customer service, not through the gateway.

## 6. Out of scope

Authentication, a message broker, running the APIs inside Kubernetes, splitting deals and tasks into their own services.

## 7. Risks

- **Memory:** the dev machine has hit critically low memory. New Postgres pods get small memory limits (~256 MB). Remove the old `postgres` pod once phase 1 is verified. Fallback if still too tight: separate databases and logins on one Postgres server (weaker isolation, much lighter).
- **Old `seerm` database:** kept until phase 1 is verified; deleted only with explicit approval.
