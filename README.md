# 🎖️ Gurudev Defence Academy

A full-stack coaching-institute web application for **Gurudev Defence Academy, Raebareli** — built with **ASP.NET Core 9 MVC**, **Entity Framework Core**, and **PostgreSQL (Neon)**.

It powers a public marketing site, an email-OTP student portal (with a batch-based classroom + live chat + embedded class videos), and a complete admin panel to manage courses, batches, study material and enquiries.

> **Address:** Degree College, Chauraha Awas Vikas Colony, Indira Nagar, Raebareli-229001, Uttar Pradesh
> **Programs:** Agniveer · Indian Airforce / Defence Exams · Class 9–12 (Physics, Chemistry, Maths, English)

---

## 📑 Table of Contents
1. [Tech Stack](#-tech-stack)
2. [High-Level Architecture](#-high-level-architecture)
3. [Application Flow](#-application-flow)
4. [Functionality](#-functionality)
5. [Security](#-security)
6. [Project Structure](#-project-structure)
7. [Data Model](#-data-model)
8. [Configuration](#-configuration)
9. [Getting Started](#-getting-started)
10. [Default Credentials](#-default-credentials)

---

## 🧰 Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core **9.0** MVC (C#) |
| ORM | Entity Framework Core 9 (code-first migrations) |
| Database | PostgreSQL — hosted on **Neon** |
| Auth | Custom session-based auth + **BCrypt** password hashing + **email OTP** |
| Email | SMTP (`System.Net.Mail`) with HTML templates |
| Key storage | ASP.NET **Data Protection** keys persisted in the DB |
| Front-end | Razor Views + Bootstrap + custom CSS (responsive, **no inline CSS**) |
| Logo | Hand-crafted **SVG** (scalable, no image asset needed) |

---

## 🏗 High-Level Architecture

The app uses a clean, layered structure with **generic base patterns** so every entity gets data-access and business logic for free.

```
Browser
   │
   ▼
Controllers (Public / Areas:User / Areas:Admin)
   │
   ▼
Services        ◄── IBaseService<T> / BaseService<T>   (business layer)
   │
   ▼
Repositories    ◄── IBaseRepository<T> / BaseRepository<T>  (data access)
   │
   ▼
AppDbContext (EF Core)  ──►  PostgreSQL (Neon)
```

- **`IBaseRepository<T>` / `BaseRepository<T>`** — generic CRUD (`GetAll`, `GetById`, `Add`, `Update`, `Delete`, `Query`, …).
- **`IBaseService<T>` / `BaseService<T>`** — generic business layer on top of the repository.
- **Concrete services** add domain logic: `UserService` (auth/registration), `CourseService` (categories + courses), `EmailService` (SMTP + templates), `OtpService` (in-memory 6-digit codes).

### Three separated areas, three layouts
| Area | Layout | Audience |
|------|--------|----------|
| Public (root) | `_Layout.cshtml` | Visitors |
| `Areas/User` | `_UserLayout.cshtml` | Logged-in students |
| `Areas/Admin` | `_AdminLayout.cshtml` | Admin / SuperAdmin |

Common UI is factored into partials (`_Navbar`, `_Footer`).

---

## 🔄 Application Flow

### Visitor → Student
```
Home / Courses / Study Material
        │
        ├── "Join Course" form ─────────► saved as JoinRequest (admin inbox)
        │
        └── Register ──► email OTP ──► Verify ──► account created ──► Student Dashboard
                                                            │
                          ┌─────────────────────────────────┼───────────────────────┐
                          ▼                                  ▼                       ▼
                     Classroom                           Profile                Study Material
            (YouTube videos for the                (set batch & class)        (download PDFs)
             student's batch + live
                  batch chat)
```

### Registration / Login (students)
1. **Register** → details validated → registration stashed in session → **OTP emailed**.
2. **Verify** → OTP checked → `AppUser` created (BCrypt-hashed password) → welcome email → signed in.
3. **Login** → email looked up → BCrypt verify → session established. Specific errors for *"no account"* vs *"wrong password"*.

### Admin
```
/admin/login ──► session (admin_role) ──► /admin/dashboard
     │
     ├── Categories  (add / edit / delete)
     ├── Courses     (add / edit / delete, per category)
     ├── Batches     (create student groups)
     ├── Class Videos(embed YouTube links per batch)
     ├── Study PDFs  (upload PDF or link, free/paid)
     ├── Join Requests (status: new → contacted → enrolled → closed)
     ├── Contact Messages (auto-marked read)
     └── Students    (assign batch, block/unblock)
```

---

## ✨ Functionality

### Public site
- **Home** — hero, why-us, dynamic course listing, CTAs.
- **Courses** — categories with their courses (icons, duration, fees).
- **Study Material** — public list of free/paid PDFs.
- **Join Course** — admission enquiry form (name, phone, email, course, message).
- **Contact Us** — message form + academy address/phone/email; also emails the academy.

### Student portal (`/student/*`, login required)
- **Dashboard** — greeting, batch info, quick stats and shortcuts.
- **Classroom** — embeds the YouTube class videos assigned to the student's **batch**, plus a **batch-only live chat** (4-second polling, HTML-escaped messages).
- **Profile** — update name/phone, **select batch & class/track** (Class 9–12 / Agniveer / Airforce…).

### Admin panel (`/admin/*`, admin login required)
- **Dashboard** — counts (categories, courses, students, batches), new-enquiry/unread badges, recent join requests.
- **Course Categories & Courses** — full CRUD with auto-slugging, ordering, active toggle.
- **Batches** — create/manage student groups (class level + year).
- **Class Videos** — add YouTube links/IDs per batch (auto-normalised to embed URLs).
- **Study PDFs** — upload a PDF (stored under `wwwroot/uploads`) or link an external URL; free or priced.
- **Join Requests** — view enquiries, change status, delete.
- **Contact Messages** — inbox, auto-marked read, delete.
- **Students** — list, assign to a batch, block/unblock.

### Emails (HTML templates in `/EmailTemplates`)
- `Otp.html` — verification code.
- `Welcome.html` — post-signup welcome with dashboard link.
- Templates use `{{Placeholder}}` tokens replaced at send time.

---

## 🔐 Security

| Area | Measure |
|------|---------|
| **Passwords** | Hashed with **BCrypt** (`BCrypt.Net-Next`) — never stored in plain text. |
| **Email verification** | 6-digit **OTP** (10-min expiry, in-memory) required to create an account. |
| **Sessions** | `HttpOnly` cookies, `SameSite=Lax`, `SecurePolicy=SameAsRequest` (Secure flag on HTTPS). 3-hour idle timeout. |
| **CSRF** | `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` on **every** state-changing POST (including the AJAX chat via `RequestVerificationToken` header). |
| **Authorization** | `[UserAuth]` and `[AdminAuth]` action filters gate the student and admin areas; admin requires role `admin`/`superadmin`. |
| **Security headers** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy`, `Permissions-Policy`, and a **Content-Security-Policy** (only allows YouTube in frames, Google Fonts for styles/fonts). |
| **Transport** | HTTPS redirection + **HSTS** in production; trusts `X-Forwarded-*` behind a reverse proxy. |
| **XSS** | Chat messages are `HtmlEncode`d on the server and escaped again on the client. Razor auto-encodes output. |
| **Open redirect** | `returnUrl` validated with `Url.IsLocalUrl` before redirecting. |
| **Data Protection keys** | Persisted in the DB so cookies/tokens survive restarts and scale across instances. |
| **File uploads** | Study uploads restricted to `.pdf`, size-limited, stored with random GUID filenames. |
| **Admin pages** | Marked `noindex, nofollow`. |

---

## 📁 Project Structure

```
GurudevDefenceAcademy/
├── Program.cs                     # DI, middleware, security headers, routes, DB migrate+seed
├── appsettings.json               # Connection string, SMTP, admin seed, academy info
│
├── Models/
│   ├── Entities/                  # AppUser, CourseCategory, Course, Batch,
│   │                              # YoutubeVideo, ChatMessage, StudyPdf,
│   │                              # JoinRequest, ContactMessage
│   └── ViewModels/                # Login, Register, JoinRequest, Contact, Profile VMs
│
├── Data/
│   ├── AppDbContext.cs            # DbSets + relationships + DataProtectionKeys
│   ├── DbSeeder.cs                # Seeds admin, courses, starter batch
│   └── Migrations/                # EF Core migrations
│
├── Repositories/Base/            # IBaseRepository<T> / BaseRepository<T>
├── Services/
│   ├── Base/                      # IBaseService<T> / BaseService<T>
│   ├── UserService.cs             # Auth, registration, password hashing
│   ├── CourseService.cs           # Categories + courses
│   ├── EmailService.cs            # SMTP + template rendering
│   └── OtpService.cs              # In-memory OTP
│
├── Middleware/
│   └── AuthAttributes.cs          # [UserAuth], [AdminAuth] filters
│
├── Controllers/                   # PUBLIC: HomeController, AccountController
├── Areas/
│   ├── User/                      # StudentController (dashboard, classroom, profile, chat API)
│   └── Admin/                     # Admin, Categories, Courses, Batches,
│                                  # Videos, Study, Inbox controllers
│
├── Views/                         # Public + shared layouts/partials
│   └── Shared/                    # _Layout, _UserLayout, _AdminLayout, _Navbar, _Footer
│
├── EmailTemplates/                # Otp.html, Welcome.html
└── wwwroot/
    ├── css/                       # site.css, dashboard.css, admin.css  (no inline CSS)
    ├── js/site.js                 # pw-toggle, toast, confirm, mobile nav
    ├── img/logo.svg               # SVG academy logo
    └── uploads/                   # uploaded study PDFs
```

---

## 🗃 Data Model

```
CourseCategory 1───* Course
Batch 1───* AppUser        (a student belongs to a batch)
Batch 1───* YoutubeVideo   (class videos shown in that batch's classroom)
Batch 1───* ChatMessage    (batch-scoped chat)
CourseCategory 1───* StudyPdf (optional)
JoinRequest, ContactMessage  (standalone enquiry records)
```

`AppUser.Role` ∈ `{ user, admin, superadmin }`.

---

## ⚙ Configuration

All settings live in **`appsettings.json`**:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Smtp": {
    "Host": "smtp.gmail.com", "Port": "587",
    "User": "<your-email>", "Password": "<app-password>",
    "From": "<your-email>", "FromName": "Gurudev Defence Academy"
  },
  "Admin": {
    "Email": "admin@gurudevdefence.in", "Password": "admin123", "Name": "Academy Admin"
  },
  "Academy": {
    "Name": "Gurudev Defence Academy",
    "Address": "Degree College, Chauraha Awas Vikas Colony, Indira Nagar, Raebareli-229001, Uttar Pradesh",
    "Phone": "", "Email": "shivsrivas1432@gmail.com",
    "YoutubeChannel": "https://www.youtube.com/@baljeetsir723/videos"
  }
}
```

> **SMTP is optional for local dev** — if left blank, OTP/welcome emails are skipped (logged as a warning) so the app still runs.

---

## 🚀 Getting Started

```bash
# 1. Restore & build
dotnet build

# 2. (Migrations are already created and auto-applied on startup.)
#    To add a new one after changing entities:
dotnet ef migrations add <Name>

# 3. Run
dotnet run            # → http://localhost:5174
```

On first run the app **auto-migrates** the database and **seeds** the admin user, course categories/courses, and a starter batch.

| Page | URL |
|------|-----|
| Public site | `/` |
| Student login / register | `/account/login`, `/account/register` |
| Student dashboard | `/student/dashboard` |
| Admin panel | `/admin/login` → `/admin/dashboard` |

---

## 🔑 Default Credentials

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | `admin@gurudevdefence.in` | `admin123` |

> ⚠️ **Change this immediately** in production (update `appsettings.json` before first run, or create a new admin and remove the default).

---

## 📝 Notes & Next Steps
- Set a real **`Academy:Phone`** in `appsettings.json` (shown in footer + contact page).
- Configure **SMTP** to enable OTP and welcome emails.
- Add class videos per batch from **Admin → Class Videos** (paste links from the academy's YouTube channel).
- Assign new students to a **batch** (Admin → Students) so their classroom and chat unlock.

---

© 2026 Gurudev Defence Academy, Raebareli. *Discipline • Dedication • Success.*
