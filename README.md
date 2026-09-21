# BreakThoughCV

[![Case Study](https://img.shields.io/badge/Case_Study-BreakThroughCV-007ACC?style=flat-square&logo=vercel)](https://thienhn0910.vercel.app/projects/breakthroughcv-aipowered-job-candidate-cv-optimization-platform)
[![Author](https://img.shields.io/badge/Author-ThienHN-4FC08D?style=flat-square)](https://thienhn0910.vercel.app/)

> **BreakThroughCV** là nền tảng tối ưu hóa CV ứng viên ứng dụng trí tuệ nhân tạo (AI-Powered Job Candidate CV Optimization Platform), kết hợp backend .NET 8 Web API, MongoDB, Google Gemini AI và frontend Vue 3.  
> Được thiết kế và phát triển bởi [ThienHN](https://thienhn0910.vercel.app/).

## Structure
- `backend/`: .NET 8 Web API + MongoDB + Cloudinary + Gemini
- `frontend/`: Vue 3 + Vite + Pinia + Tailwind
- `PRD.md`: Product requirements and architecture

## Setup
### Backend
1. Copy `backend/appsettings.Example.json` values into your local `backend/appsettings.json`.
   - Nếu muốn bật thanh toán PayOS để dùng AI: điền `PayOsSettings` (ClientId/ApiKey/ChecksumKey/ReturnUrl/CancelUrl) và cấu hình webhook trên my.payos.vn trỏ về `POST /api/payments/payos/webhook`.
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

## 🌐 Case Study & Showcase

- 📌 **Chi tiết Case Study dự án**: [BreakThroughCV — AI-Powered Job Candidate CV Optimization Platform](https://thienhn0910.vercel.app/projects/breakthroughcv-aipowered-job-candidate-cv-optimization-platform)
- 👨‍💻 **Portfolio tác giả**: [ThienHN (thienhn0910.vercel.app)](https://thienhn0910.vercel.app/) | [Xem CV trực tuyến](https://thienhn0910.vercel.app/cv)
- 🚀 **Khám phá thêm dự án khác**: [Portfolio Projects Showcase](https://thienhn0910.vercel.app/projects)
