# BreakThroughCV Test Report

Date: 2026-05-18
Environment:
- Backend: http://localhost:5187
- Frontend build: Vite production build

## 1. Build Validation
1. Backend build: PASS
- Command: `dotnet build backend/BreakThroughCV.API.csproj`
2. Frontend build: PASS
- Command: `pnpm run build`

## 2. Feature Smoke Tests
Reference checklist: `docs/FEATURES.md`

1. Auth me with recruiter token: PASS
- Endpoint: `GET /api/auth/me`

2. Public categories list: PASS
- Endpoint: `GET /api/categories`

3. Company upsert with proper multipart form: PASS
- Endpoint: `POST /api/companies`
- Note: Form-url-encoded request returns `415` by design because this endpoint expects multipart/form-data.

4. Get recruiter company: PASS
- Endpoint: `GET /api/companies/my`

5. Create job with empty categoryId: PASS
- Endpoint: `POST /api/jobs`
- Verified response has `categoryId: null` and no server `500`.

6. Update job: PASS
- Endpoint: `PUT /api/jobs/{id}`

7. List jobs by company: PASS
- Endpoint: `GET /api/jobs/company/{companyId}`

8. Public jobs list: PASS
- Endpoint: `GET /api/jobs`

9. Update role to candidate: PASS
- Endpoint: `PUT /api/auth/update-role`

10. Candidate blocked from recruiter action: PASS (expected forbidden)
- Endpoint: `POST /api/jobs`
- Result: `403`

11. Candidate applications list: PASS
- Endpoint: `GET /api/applications/my`

12. AI suggest jobs: PASS
- Endpoint: `POST /api/ai/suggest-jobs`

13. Update role back to recruiter: PASS
- Endpoint: `PUT /api/auth/update-role`

14. Recruiter applications by job: PASS
- Endpoint: `GET /api/applications/job/{jobId}`

15. Delete test job: PASS
- Endpoint: `DELETE /api/jobs/{id}`

## 3. UI Validation
1. Layout and theme updates compile successfully: PASS
2. Updated pages render through production build: PASS
3. Global 401 interceptor behavior implemented in API client: PASS (code-level validation)

## 4. Fixes Verified
1. `categoryId` empty-string serialization crash fixed in jobs API.
2. Same `categoryId` normalization added in company upsert API.
3. Frontend global 401 handling implemented.
4. PostCSS import-order warning resolved.
