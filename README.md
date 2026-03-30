<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Redis-alpine-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

<h1 align="center">🎓 VietEdu — Backend API</h1>

<p align="center">
  <strong>A modern, AI-powered Learning Management System backend built with ASP.NET Core 9 and Modular Monolith architecture.</strong>
</p>

<p align="center">
  <a href="#-key-features">Features</a> •
  <a href="#-architecture">Architecture</a> •
  <a href="#-getting-started">Getting Started</a> •
  <a href="#-environment-configuration">Configuration</a> •
  <a href="#-api-reference">API</a> •
  <a href="#-contributing">Contributing</a> •
  <a href="#-license">License</a>
</p>

---

## 📖 Introduction

**VietEdu** is a full-featured online learning platform backend designed to power modern e-learning experiences. It provides RESTful APIs for course management, lecture delivery, quiz assessment, payment processing, and real-time notifications — all enhanced with AI-powered capabilities like automatic video transcription and intelligent content analysis.

Built with a **Modular Monolith** approach, VietEdu balances the simplicity of a monolithic deployment with the clean separation of concerns found in microservices. Each business domain (Courses, Lectures, Accounts, Payments, Cart) lives in its own self-contained module with dedicated API, Application, Domain, and Infrastructure layers.

### Why VietEdu?

| Problem | VietEdu's Solution |
|---|---|
| Complex microservice orchestration | Modular Monolith — single deployment, clean boundaries |
| Manual content transcription | AI-powered automatic video transcription & subtitle generation |
| Slow course search | Full-text search powered by Apache Lucene.NET |
| Lack of real-time interaction | SignalR-based live notifications |
| Rigid payment systems | Multi-provider payment support (VnPay, SePay) |

---

## ✨ Key Features

### 🎯 Core Platform
- **Course Management** — Create, publish, and manage courses with approval workflows
- **Lecture System** — Upload videos, documents, and organize content with drag-and-drop ordering
- **Quiz Engine** — Build quizzes with multiple question types, attempt tracking, and scoring
- **Shopping Cart & Checkout** — Full e-commerce flow for course purchases
- **User Roles** — Role-based access for Students, Instructors, and Admins

### 🤖 AI & Intelligence
- **Video Transcription** — Automatic speech-to-text via iFlyTek ASR and Google Cloud Speech
- **Subtitle Generation** — Auto-generate subtitles for lecture videos
- **Content Analysis** — AI-powered content analysis via Google Gemini
- **Smart Prompts** — Configurable AI prompt templates for content generation

### 🔍 Search & Discovery
- **Full-Text Search** — Apache Lucene.NET-powered course and content search
- **Tag System** — Organize and discover courses through tags and categories

### 💳 Payments
- **VnPay Integration** — Vietnam's leading payment gateway
- **SePay Integration** — QR-based bank transfer payments
- **Transaction Tracking** — Complete payment lifecycle management

### 🔔 Real-Time
- **SignalR Notifications** — Instant push notifications to connected clients
- **Live Updates** — Real-time course enrollment and status updates

### 🏗️ Infrastructure
- **JWT Authentication** — Secure token-based auth with refresh tokens
- **Google OAuth** — Social login with Google
- **Cloudinary CDN** — Optimized media storage and delivery
- **Redis Caching** — High-performance distributed caching
- **Hangfire** — Reliable background job processing
- **Serilog** — Structured logging with file and console sinks
- **i18n** — Full internationalization support (Vietnamese & English)

---

## 🏛️ Architecture

VietEdu follows a **Modular Monolith** architecture pattern. Each service module is self-contained with its own layers, but all modules share a single database and deployment unit.

### High-Level Overview

```mermaid
graph TB
    Client["🌐 Client Applications"]

    subgraph API_GATEWAY["ASP.NET Core 9 Host"]
        direction TB
        MW["Middleware Pipeline<br/>CORS · Auth · Localization"]

        subgraph MODULES["Service Modules"]
            direction LR
            AS["👤 Account<br/>Service"]
            CS["📚 Course<br/>Service"]
            LS["🎬 Lecture<br/>Service"]
            PS["💳 Payment<br/>Service"]
            CRS["🛒 Cart<br/>Service"]
        end

        subgraph SHARED["Shared Kernel"]
            direction LR
            DB["AppDbContext"]
            ENT["Entities"]
            RES["Resources (i18n)"]
            EXT["Extensions"]
        end
    end

    subgraph EXTERNAL["External Services"]
        direction LR
        PG["🐘 PostgreSQL"]
        RD["⚡ Redis"]
        CL["☁️ Cloudinary"]
        GM["🤖 Gemini AI"]
        VP["💰 VnPay"]
        SP["💰 SePay"]
        GA["🔐 Google Auth"]
    end

    Client --> MW
    MW --> MODULES
    MODULES --> SHARED
    SHARED --> PG
    SHARED --> RD
    CS --> CL
    CS --> GM
    PS --> VP
    PS --> SP
    AS --> GA
    LS --> CL

    style API_GATEWAY fill:#1a1a2e,stroke:#16213e,color:#e6e6e6
    style MODULES fill:#0f3460,stroke:#16213e,color:#e6e6e6
    style SHARED fill:#533483,stroke:#16213e,color:#e6e6e6
    style EXTERNAL fill:#1a1a2e,stroke:#e94560,color:#e6e6e6
```

### Module Architecture (per Service)

Each service module follows **Clean Architecture** principles:

```mermaid
graph TD
    subgraph MODULE["Service Module"]
        direction TB

        subgraph API_LAYER["🌐 API Layer"]
            CTRL["Controllers"]
        end

        subgraph APP_LAYER["⚙️ Application Layer"]
            SVC["Services / Use Cases"]
            DTO["DTOs"]
            INT["Interfaces"]
        end

        subgraph DOMAIN_LAYER["🏛️ Domain Layer"]
            ENT["Entities"]
            ENUM["Enums"]
        end

        subgraph INFRA_LAYER["🔧 Infrastructure Layer"]
            REPO["Repositories"]
            EXT_SVC["External Services"]
        end
    end

    CTRL --> SVC
    SVC --> INT
    SVC --> DTO
    INT -.->|implemented by| REPO
    REPO --> ENT
    EXT_SVC --> ENT

    style API_LAYER fill:#e94560,stroke:#333,color:#fff
    style APP_LAYER fill:#0f3460,stroke:#333,color:#fff
    style DOMAIN_LAYER fill:#533483,stroke:#333,color:#fff
    style INFRA_LAYER fill:#16213e,stroke:#333,color:#fff
```

### Request Lifecycle

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Middleware
    participant CTRL as Controller
    participant SVC as Service
    participant REPO as Repository
    participant DB as PostgreSQL

    C->>MW: HTTP Request
    MW->>MW: CORS Validation
    MW->>MW: JWT Authentication
    MW->>MW: Request Localization
    MW->>CTRL: Route to Controller
    CTRL->>SVC: Call Application Service
    SVC->>REPO: Query/Command
    REPO->>DB: EF Core Query
    DB-->>REPO: Data
    REPO-->>SVC: Domain Entity
    SVC-->>CTRL: DTO / Result
    CTRL-->>C: ApiResponse (JSON)
```

### API Response Format

All endpoints return a consistent `ApiResponse` envelope:

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation completed successfully",
  "data": { }
}
```

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Runtime & build tooling |
| [PostgreSQL](https://www.postgresql.org/download/) | 14+ | Primary database |
| [Redis](https://redis.io/download) | 6+ | Caching & Hangfire storage |
| [Docker](https://docs.docker.com/get-docker/) | 20+ | *(Optional)* Containerized deployment |
| [Git](https://git-scm.com/) | 2.30+ | Version control |

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/viettri2004/DACN_BE.git
cd DACN_BE
```

#### 2. Configure Environment Variables

Create a `.env` file inside the `src/` directory:

```bash
cp src/.env.example src/.env
```

Then edit `src/.env` with your actual credentials (see [Environment Configuration](#-environment-configuration) for details).

#### 3. Restore Dependencies

```bash
cd src
dotnet restore
```

#### 4. Apply Database Migrations

```bash
dotnet ef database update
```

> **Note:** Make sure your `POSTGRES_CONNECTION` in `.env` points to a running PostgreSQL instance before running migrations.

#### 5. Start Redis

```bash
# Using Docker (recommended)
docker run -d --name redis-cache -p 6379:6379 redis:alpine

# Or use a local Redis installation
redis-server
```

### Running the Project

#### Option A: .NET CLI (Development)

```bash
cd src
dotnet run
```

The API will be available at:
- **HTTP:** `http://localhost:5223`
- **Swagger UI:** `http://localhost:5223/swagger`
- **Hangfire Dashboard:** `http://localhost:5223/hangfire`

#### Option B: Docker Compose (Production-like)

```bash
# Build and start all services
docker compose up --build

# Run in detached mode
docker compose up --build -d

# View logs
docker compose logs -f server
```

This will start:
- **VietEdu API** on port `5223`
- **Redis** container for caching and background jobs

#### Option C: Visual Studio

1. Open `DACN_BE.sln`
2. Set `src` as the startup project
3. Press `F5` or click **Run**

### Verifying the Installation

Once the server is running, verify it's working:

```bash
# Health check via Swagger
curl http://localhost:5223/swagger/v1/swagger.json

# Or simply open in your browser
# http://localhost:5223/swagger
```

---

## ⚙️ Environment Configuration

VietEdu uses a `.env` file in the `src/` directory for sensitive configuration. The app loads these variables at startup via `DotNetEnv`.

### Required Variables

Create `src/.env` with the following variables:

```env
# ─── Database & Cache ────────────────────────────────────────
POSTGRES_CONNECTION="Host=localhost;Port=5432;Database=vietedu;Username=postgres;Password=your_password;"
REDIS_CONNECTION="localhost:6379"

# ─── Media Storage ───────────────────────────────────────────
CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME

# ─── AI Services ─────────────────────────────────────────────
GEMINI_API_KEY="your_gemini_api_key"
AI_SERVER_URL="http://your-ai-server:8000/transcribe-from-cloudinary"

# ─── Security ────────────────────────────────────────────────
JWT__SigningKey="your_jwt_signing_key_minimum_32_chars"

# ─── Email (SMTP) ────────────────────────────────────────────
Email__Password="your_smtp_app_password"

# ─── Payment Gateways ────────────────────────────────────────
VnPay__HashSecret="your_vnpay_hash_secret"

# ─── Google OAuth ─────────────────────────────────────────────
Google__ClientId="your_google_client_id.apps.googleusercontent.com"
Google__ClientSecret="your_google_client_secret"
```

### Application Settings (`appsettings.json`)

Non-secret configuration lives in `src/appsettings.json`:

| Section | Key | Description | Default |
|---|---|---|---|
| `JWT` | `Issuer` | Token issuer URL | `http://localhost:5223` |
| `JWT` | `Audience` | Token audience URL | `http://localhost:5223` |
| `JWT` | `AccessTokenExpireMinutes` | Token TTL in minutes | `30` |
| `Email` | `From` | Sender email address | — |
| `VnPay` | `TmnCode` | VnPay terminal code | — |
| `VnPay` | `BaseUrl` | VnPay gateway URL | Sandbox URL |
| `VnPay` | `ReturnUrl` | Payment return callback | — |
| `Google` | `AuthUri` | Google OAuth endpoint | Google default |
| `Google` | `RedirectUri` | OAuth callback URL | — |
| `FrontendUrls` | `PaymentSuccess` | Redirect after payment success | `http://localhost:5173/...` |
| `FrontendUrls` | `PaymentFail` | Redirect after payment failure | `http://localhost:5173/...` |

### Docker Compose Environment

When running via Docker Compose, environment variables are passed through the `compose.yaml` file:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTION}
  - ConnectionStrings__Redis=${REDIS_CONNECTION}
  - Cloudinary__Url=${CLOUDINARY_URL}
  - Gemini__ApiKey=${GEMINI_API_KEY}
  - AI__ServerUrl=${AI_SERVER_URL}
```

---

## 📁 Folder Structure

```
DACN_BE/
├── 📄 DACN_BE.sln                    # Visual Studio solution file
├── 📄 compose.yaml                   # Docker Compose configuration
├── 📄 README.md                      # This file
│
└── src/                              # Application source code
    ├── 📄 Program.cs                 # Application entry point & DI configuration
    ├── 📄 src.csproj                 # Project file & NuGet dependencies
    ├── 📄 .env                       # Environment variables (not committed)
    ├── 📄 Dockerfile                 # Multi-stage Docker build
    ├── 📄 appsettings.json           # Non-secret application config
    │
    ├── Services/                     # 🔷 Business domain modules
    │   ├── AccountService/           # 👤 User management & authentication
    │   │   ├── API/
    │   │   │   └── Controllers/
    │   │   │       ├── AccountController.cs      # Auth, profile, user CRUD
    │   │   │       ├── DashboardController.cs    # Admin analytics
    │   │   │       └── NotificationController.cs # Push notifications
    │   │   ├── Application/          # DTOs, interfaces, services
    │   │   ├── Domain/
    │   │   │   ├── Entities/         # User, Student, Instructor, Admin, Notification
    │   │   │   └── Enums/
    │   │   └── Infrastructure/       # Repos, Email, Google OAuth, OTP
    │   │
    │   ├── CourseService/            # 📚 Course lifecycle management
    │   │   ├── API/
    │   │   │   └── Controllers/
    │   │   │       ├── CourseController.cs   # Course CRUD & enrollment
    │   │   │       ├── AiController.cs       # AI content generation
    │   │   │       └── TagController.cs      # Tag management
    │   │   ├── Application/
    │   │   ├── Domain/
    │   │   │   ├── Entities/         # Course, Tag, Enrollment, Comment, CourseRequest
    │   │   │   └── Enums/
    │   │   └── Infrastructure/
    │   │       ├── Repositories/
    │   │       ├── Prompts/          # AI prompt templates (.txt)
    │   │       ├── LmsAiService.cs   # Gemini AI integration
    │   │       └── VideoProcessingService.cs
    │   │
    │   ├── LectureService/           # 🎬 Lecture & quiz management
    │   │   ├── API/
    │   │   │   └── Controllers/
    │   │   │       ├── LectureController.cs  # Lecture CRUD & video upload
    │   │   │       └── QuizController.cs     # Quiz management & attempts
    │   │   ├── Application/
    │   │   ├── Domain/
    │   │   │   └── Entities/         # Lecture, LectureVideo, Quiz, Question, QuizAttempt
    │   │   └── Infrastructure/
    │   │
    │   ├── PaymentService/           # 💳 Payment processing
    │   │   ├── API/
    │   │   │   └── Controllers/
    │   │   │       └── PaymentController.cs  # VnPay & SePay integration
    │   │   ├── Application/
    │   │   ├── Domain/
    │   │   └── Infrastructure/       # VnPayService, SepayService
    │   │
    │   └── CartService/              # 🛒 Shopping cart
    │       ├── API/
    │       │   └── Controllers/
    │       │       └── CartController.cs
    │       ├── Application/
    │       ├── Domain/
    │       └── Infrastructure/
    │
    ├── Shared/                       # 🔷 Cross-cutting concerns
    │   ├── Application/
    │   │   ├── Extension/
    │   │   │   └── ApiResponseExtension.cs   # .ToActionResult() helper
    │   │   └── Interfaces/
    │   ├── Domain/
    │   │   └── Entities/
    │   │       ├── ApiResponse.cs            # Standard API envelope
    │   │       ├── PagedResult.cs            # Pagination wrapper
    │   │       └── SharedResources.cs        # i18n resource accessor
    │   ├── Infrastructure/
    │   │   ├── AppDbContext.cs               # EF Core database context
    │   │   ├── DbInitializer.cs             # Seed data
    │   │   ├── Cloudiary/                   # Cloudinary media service
    │   │   ├── Hubs/
    │   │   │   └── NotificationHub.cs       # SignalR real-time hub
    │   │   └── iFlyTek/                     # Speech-to-text integration
    │   └── Resources/
    │       ├── SharedResources.vi.resx       # Vietnamese translations
    │       └── SharedResources.en.resx       # English translations
    │
    ├── Migrations/                   # EF Core database migrations
    ├── Properties/
    │   └── launchSettings.json       # Dev server launch profiles
    └── lucene_index/                 # Lucene.NET search index data
```

---

## 🔌 API Reference

### Base URL

```
http://localhost:5223/api
```

### Authentication

All protected endpoints require a JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <your_jwt_token>
```

### Localization

Set the response language via the `Accept-Language` header:

```
Accept-Language: vi    # Vietnamese (default)
Accept-Language: en    # English
```

### Available Endpoints

| Module | Endpoint | Methods | Description |
|---|---|---|---|
| **Account** | `/api/Account` | `GET` `POST` `PUT` `DELETE` | User management, registration, login |
| **Account** | `/api/Account/google-*` | `GET` `POST` | Google OAuth flow |
| **Dashboard** | `/api/Dashboard` | `GET` | Admin analytics & statistics |
| **Notification** | `/api/Notification` | `GET` `POST` `PUT` | Push notifications management |
| **Course** | `/api/Course` | `GET` `POST` `PUT` `DELETE` | Course CRUD & enrollment |
| **AI** | `/api/Ai` | `POST` | AI-powered content generation |
| **Tag** | `/api/Tag` | `GET` `POST` `PUT` `DELETE` | Course tag management |
| **Lecture** | `/api/Lecture` | `GET` `POST` `PUT` `DELETE` | Lectures, videos, documents |
| **Quiz** | `/api/Quiz` | `GET` `POST` `PUT` `DELETE` | Quiz management & attempts |
| **Payment** | `/api/Payment` | `GET` `POST` | Payment processing & callbacks |
| **Cart** | `/api/Cart` | `GET` `POST` `DELETE` | Shopping cart operations |

> 📝 **Full API documentation** is available at `/swagger` when the server is running.

---

## 🧪 Tech Stack

| Category | Technology |
|---|---|
| **Runtime** | .NET 9.0 (ASP.NET Core) |
| **Language** | C# 13 |
| **Database** | PostgreSQL via EF Core 9 |
| **Cache** | Redis (StackExchange.Redis) |
| **Auth** | ASP.NET Identity + JWT Bearer + Google OAuth |
| **Real-Time** | SignalR |
| **Search** | Apache Lucene.NET 4.8 |
| **Background Jobs** | Hangfire (Redis storage) |
| **Media** | Cloudinary CDN |
| **AI** | Google Gemini (GenAI SDK), iFlyTek ASR |
| **Payments** | VnPay, SePay |
| **Logging** | Serilog (Console + File sinks) |
| **API Docs** | Swashbuckle (Swagger/OpenAPI) |
| **Mapping** | AutoMapper |
| **Containerization** | Docker + Docker Compose |
| **i18n** | Microsoft.Extensions.Localization (.resx) |

---

## 🤝 Contributing

We welcome contributions! Here's how you can help make VietEdu better.

### Getting Started

1. **Fork** the repository
2. **Clone** your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/DACN_BE.git
   ```
3. **Create a feature branch:**
   ```bash
   git checkout -b feature/amazing-feature
   ```
4. **Make your changes** following the conventions below
5. **Commit** with a descriptive message:
   ```bash
   git commit -m "feat(course): add bulk enrollment endpoint"
   ```
6. **Push** to your fork:
   ```bash
   git push origin feature/amazing-feature
   ```
7. **Open a Pull Request** against the `main` branch

### Commit Convention

We follow [Conventional Commits](https://www.conventionalcommits.org/):

| Prefix | Purpose | Example |
|---|---|---|
| `feat` | New feature | `feat(lecture): add subtitle generation` |
| `fix` | Bug fix | `fix(payment): handle VnPay timeout` |
| `docs` | Documentation | `docs: update API reference` |
| `refactor` | Code restructuring | `refactor(account): extract token logic` |
| `test` | Tests | `test(quiz): add attempt scoring tests` |
| `chore` | Maintenance | `chore: update NuGet packages` |

### Code Conventions

- **Controllers** — Return `ActionResult<ApiResponse>` and use `.ToActionResult()`
- **New messages** — Add to `SharedResources.resx` and use `IStringLocalizer`
- **Module structure** — Follow the 4-layer pattern: `API/` → `Application/` → `Domain/` → `Infrastructure/`
- **Dependency Injection** — Register services in `Program.cs` inside the `ConfigureDI` method
- **Naming** — Use PascalCase for classes/methods, camelCase for local variables

### Reporting Issues

Found a bug? Please [open an issue](https://github.com/viettri2004/DACN_BE/issues) with:
- A clear and descriptive title
- Steps to reproduce the behavior
- Expected vs actual behavior
- Screenshots or logs if applicable

---

## 📜 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 VietEdu

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

---

## 🗺️ Roadmap

### ✅ Delivered

- [x] Modular Monolith architecture
- [x] JWT & Google OAuth authentication
- [x] Course management with approval workflow
- [x] Lecture & video management with Cloudinary
- [x] Quiz engine with attempt tracking
- [x] Shopping cart & payment integration (VnPay, SePay)
- [x] AI-powered video transcription & subtitle generation
- [x] Full-text search with Lucene.NET
- [x] Real-time notifications via SignalR
- [x] Internationalization (Vietnamese & English)
- [x] Background job processing with Hangfire
- [x] Docker & Docker Compose deployment

### 🚧 In Progress

- [ ] Comprehensive unit & integration test suite
- [ ] Rate limiting & API throttling
- [ ] Course progress tracking & completion certificates

### 🔮 Planned

- [ ] WebSocket-based live chat for courses
- [ ] Instructor earnings dashboard & payout system
- [ ] Advanced analytics & learning path recommendations
- [ ] Mobile push notifications (FCM)
- [ ] Multi-tenant support for organizations
- [ ] GraphQL API layer
- [ ] Event sourcing for audit trails
- [ ] CI/CD pipeline with GitHub Actions
- [ ] Course reviews & rating system improvements
- [ ] Content moderation pipeline

---

<p align="center">
  Built with ❤️ by the <strong>VietEdu</strong> team
</p>
