# BreakThroughCV Feature Documentation

## 1. Authentication and Session
### 1.1 Google login
- Frontend page: `/login`
- API: `POST /api/auth/google-login`
- Behavior:
- Accepts Google `idToken`
- Creates new user on first login
- Returns JWT + user profile + role

### 1.2 Role selection
- Frontend page: `/select-role`
- API: `PUT /api/auth/update-role`
- Allowed roles: `candidate`, `recruiter`
- Returns refreshed JWT after role update

### 1.3 Session recovery and unauthorized handling
- Frontend API client now handles `401 Unauthorized` globally
- On `401`, frontend clears `localStorage` (`token`, `user`) and redirects to `/login`

## 2. Recruiter Features
### 2.1 Company management
- Frontend page: `/recruiter/company`
- APIs:
- `GET /api/companies/my`
- `POST /api/companies` (multipart/form-data)
- Supports create/update company profile
- `categoryId` handling:
- Empty value is normalized to `null`
- Invalid non-empty value returns `400` (`categoryId is not a valid ObjectId`)

### 2.2 Job management
- Frontend page: `/recruiter/jobs`
- APIs:
- `POST /api/jobs`
- `PUT /api/jobs/{id}`
- `DELETE /api/jobs/{id}`
- `GET /api/jobs/company/{companyId}`
- `categoryId` handling:
- Empty value is normalized to `null`
- Invalid non-empty value returns `400` (`categoryId is not a valid ObjectId`)

### 2.3 Candidate applications management
- Frontend page: `/recruiter/applications`
- APIs:
- `GET /api/applications/job/{jobId}`
- `PUT /api/applications/{id}/status`
- Allowed status values: `Pending`, `Reviewed`, `Accepted`, `Rejected`

## 3. Candidate Features
### 3.1 Job browsing
- Frontend page: `/jobs`
- APIs:
- `GET /api/jobs`
- `GET /api/categories`

### 3.2 CV management
- Frontend page: `/candidate/cv`
- APIs:
- `POST /api/cv/upload` (multipart/form-data)
- `GET /api/cv/my`
- `GET /api/cv/file/{userId}` (anonymous, PDF stream for preview)
- Behavior:
- Uploaded CV is saved on the backend and the saved URL points to the backend file endpoint
- PDF.js preview reads from the backend file URL, so no Cloudinary raw access is needed for preview

Additional details:
- The backend extracts text from uploaded PDF using `UglyToad.PdfPig` and stores extracted text with the application when the candidate applies. This text is used by AI features and eliminates the need for the frontend to send raw text.
- AI endpoints accept either raw `cvText` or a `cvUrl` (the backend will fetch and extract text if `cvText` is not provided).

### 3.3 AI review and suggestions
- Frontend page: `/candidate/ai-review`
- APIs:
- `POST /api/ai/suggest-jobs`
- `POST /api/ai/review-cv`
- `GET /api/ai/review-history`

Notes:
- The `POST /api/ai/*` endpoints accept `{ cvText }` or `{ cvUrl }`. If `cvUrl` is provided the server will fetch and extract text for analysis.
- AI review is candidate-facing only in the product flow.

### 3.4 Candidate applications
- API:
- `POST /api/applications` (multipart/form-data)
- `GET /api/applications/my`

## 4. Authorization Rules
- Recruiter-only APIs return `403` for candidate users
- Candidate-only APIs return `403` for recruiter users
- Unauthenticated calls to protected APIs return `401`

## 5. UI Improvements Implemented
- New visual system with reusable classes in `frontend/src/assets/main.css`
- Updated layout shell and role-aware top navigation in `frontend/src/layouts/AppLayout.vue`
- Restyled pages:
- `Login.vue`
- `SelectRole.vue`
- `Dashboard.vue`
- `JobList.vue`
- `CompanyManagement.vue`
- `JobManagement.vue`
- `CVManagement.vue`
- `AIReview.vue`
- `ApplicationManagement.vue`

## 6. Test Checklist (Feature-Based)
1. Login and role switching works and returns valid JWT.
2. Recruiter can read/update company profile.
3. Recruiter can create/update/delete jobs.
4. Empty `categoryId` does not produce server `500`.
5. Candidate cannot access recruiter-only actions.
6. Candidate can call candidate-only endpoints.
7. Public endpoints (`/jobs`, `/categories`) are reachable without auth.
8. Frontend handles `401` by clearing session and redirecting to `/login`.
