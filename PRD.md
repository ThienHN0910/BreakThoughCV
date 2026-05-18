# Breakthrough CV - Product Requirements & Architecture

## 1. Product Goal
Breakthrough CV là nền tảng kết nối ứng viên và nhà tuyển dụng, tập trung vào tối ưu CV bằng AI theo JD để tăng tỷ lệ trúng tuyển.

## 2. Personas
- Candidate: tìm việc, upload CV, review CV theo JD, apply job.
- Recruiter: quản lý công ty, tạo job, theo dõi ứng viên nộp hồ sơ.

## 3. Core Features
1. Google Login + phân vai trò (`none`, `candidate`, `recruiter`).
2. Recruiter: quản lý company/logo, CRUD jobs, xem applications.
3. Candidate: tìm việc theo danh mục/từ khóa, upload CV qua backend.
4. AI với Gemini:
   - Job suggestion: top 3 job phù hợp từ CV.
   - CV review theo JD: score, missing keywords, critical fixes, tailored suggestions.

## 4. Non-Functional Requirements
- Security: mọi file upload đi qua backend validate trước khi upload Cloudinary.
- Reliability: bắt lỗi mạng Cloudinary/Gemini, trả lỗi chuẩn thay vì crash.
- Lean architecture: tách Settings/Services/Controllers rõ ràng.

## 5. Tech Stack
- Backend: .NET 8 Web API, MongoDB.Driver, JWT, CloudinaryDotNet, HttpClient.
- Frontend: Vue 3 + Vite, Pinia, Vue Router, Tailwind CSS.
- Database: MongoDB.
- AI: Gemini model `gemini-3.1-flash-lite` (configurable).

## 6. Data Model (MongoDB Collections)
- Users: `{ Id, Email, Name, AvatarUrl, Role, CreatedAt }`
- Categories: `{ Id, Name, Slug }`
- Companies: `{ Id, RecruiterId, Name, LogoUrl, Description, CategoryId, Website }`
- Jobs: `{ Id, CompanyId, Title, CategoryId, Description, Responsibilities, MustHaveSkills, NiceToHaveSkills, MinExperienceYears, CreatedAt }`
- Applications: `{ Id, JobId, CandidateId, CvUrl, AppliedAt, Status }`
- CVReviews: `{ Id, CandidateId, JobId, Score, MissingKeywords, CriticalFixes, TailoredSuggestions, CreatedAt }`

## 7. API Boundaries
- `/api/auth/*`: login Google, update role, get profile.
- `/api/categories/*`: danh mục ngành nghề.
- `/api/companies/*`: recruiter quản lý company.
- `/api/jobs/*`: listing/filter + recruiter CRUD.
- `/api/applications/*`: candidate apply, recruiter review.
- `/api/cv/*`: candidate upload CV.
- `/api/ai/*`: suggest jobs, review CV.

## 8. Frontend Screens
- `Login.vue`, `SelectRole.vue`, `Dashboard.vue`
- Candidate: `JobList.vue`, `CVManagement.vue`, `AIReview.vue`
- Recruiter: `CompanyManagement.vue`, `JobManagement.vue`, `ApplicationManagement.vue`

## 9. Environment Configuration
- Backend: `backend/appsettings.Example.json`
- Frontend: `frontend/.env.example`
