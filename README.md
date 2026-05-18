# BreakThoughCV

## Structure
- `backend/`: .NET 8 Web API + MongoDB + Cloudinary + Gemini
- `frontend/`: Vue 3 + Vite + Pinia + Tailwind
- `PRD.md`: Product requirements and architecture

## Setup
### Backend
1. Copy `backend/appsettings.Example.json` values into your local `backend/appsettings.json`.
2. Run:
   - `cd /home/runner/work/BreakThoughCV/BreakThoughCV/backend`
   - `dotnet run`

### Frontend
1. Create `.env` from `frontend/.env.example`.
2. Run:
   - `cd /home/runner/work/BreakThoughCV/BreakThoughCV/frontend`
   - `npm install`
   - `npm run dev`

## Troubleshooting
- POST `/api/jobs` with empty `categoryId` should now be accepted and treated as `null`. If `categoryId` is provided but not a valid Mongo ObjectId, API returns `400` with message `categoryId is not a valid ObjectId`.
- `POST /api/companies` follows the same `categoryId` validation and empty-string normalization.
- Frontend now auto-handles `401 Unauthorized`: session in local storage is cleared and user is redirected to `/login`.

## Documentation
- Feature documentation: `docs/FEATURES.md`
- Latest test report: `docs/TEST_REPORT.md`

## Test Script
- Run API smoke tests: `./scripts/feature-smoke-test.ps1`
