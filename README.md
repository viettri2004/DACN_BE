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
  <a href="#-folder-structure">Folder Structure</a> •
  <a href="#-api-reference">API</a> •
  <a href="#-contributing">Contributing</a> •
  <a href="#-license">License</a>
</p>

---

## 📖 Introduction

**VietEdu** is a full-featured online learning platform backend designed to power modern e-learning experiences. It provides RESTful APIs for course management, lecture delivery, quiz assessment, payment processing, and real-time notifications — all enhanced with AI-powered capabilities like automatic video transcription and intelligent content analysis.

Built with a **Modular Monolith** approach, VietEdu balances the simplicity of a monolithic deployment with the clean separation of concerns found in microservices. Each business domain lives in its own self-contained module with dedicated API, Application, Domain, and Infrastructure layers.

### Why VietEdu?

| Problem | VietEdu's Solution |
|---|---|
| Complex microservice orchestration | Modular Monolith — single deployment, clean boundaries |
| Manual content transcription | AI-powered automatic video transcription & subtitle generation via dedicated AI Server |
| AI Content Analysis & Summary | Google Gemini AI (`gemini-3-flash-preview`) analysis using Google.GenAI SDK |
| Slow course search | Full-text search, spelling suggestions, and facet filtering powered by Apache Lucene.NET 4.8 |
| Lack of real-time interaction | SignalR-based live notifications (`/notificationHub`) |
| Rigid payment systems | Multi-provider payment support (VnPay gateway & QR-based bank transfer SePay with instant webhook validation) |

---

## ✨ Key Features

### 👤 Identity & Authentication (`IdentityService`)
- **Multi-Role User Management** — Dedicated workflows and profiles for Students, Instructors, and Admins.
- **Secure Authentication** — JWT Authentication with access tokens, refresh tokens, and Google OAuth login integration.
- **Admin Dashboard** — Centralized analytics and system metrics with distributed cache optimization.

### 📚 Course & Content Management (`ContentService`)
- **Course Lifecycle** — Create, update, and publish courses with approval workflows (Request -> Review -> Approve/Reject).
- **Rich Lecture System** — Organize lectures with video lectures, document attachments, and drag-and-drop order sequencing.
- **Quiz Engine** — Create quizzes with multiple-choice questions, options, and attempt tracking.
- **Category & Tag System** — Organize courses dynamically via tags.
- **Secure Uploads** — Direct-to-Cloudinary media upload signature generation (Image, Video, Raw documents) for client applications.

### 💬 Course Interactions (`InteractionService`)
- **Q&A Threads** — Multi-user forum threads and message replies inside lectures.
- **Comments & Reviews** — Post comments on courses.
- **Wishlist** — Save courses to a personalized student wishlist.

### 📈 Student Progress (`LearningService`)
- **Progress Tracking** — Tracks lecture completion progress for enrolled students.
- **Quiz Assessments** — Records quiz attempts, calculates results, and stores history.

### 🔔 Real-Time Notifications (`NotificationService`)
- **SignalR Hub** — Live push notifications for course events, enrollment changes, and announcement broadcasts.
- **Notification Center** — Fetch, mark as read, and manage user notifications.

### 💳 Shopping Cart & Checkout (`OrderingService`)
- **Cart Management** — Add/remove items, retrieve cart details.
- **VnPay Integration** — Seamless payment gateway redirect checkout with signature validation, return and IPN callbacks.
- **SePay QR Payment** — Dynamic bank transfer QR generation (via QRCoder) with secure webhook callback handling.

### 🔍 Search & AI Capabilities (`SearchService`)
- **Lucene Full-Text Search** — High-performance course searching, autocomplete search suggestions, and facet tagging.
- **Gemini Video Analysis** — Extract video transcripts to automatically generate summaries and study aids using the Google GenAI SDK.
- **Hangfire Worker** — Asynchronous video processing and speech-to-text transcription queues.

---

## 🏛️ Architecture

VietEdu follows a **Modular Monolith** architecture pattern. Each service module is self-contained with its own layers, but all modules share a single database and deployment unit.

### High-Level Overview

```mermaid
graph TB
    Client["🌐 Client Applications"]

    subgraph API_GATEWAY["ASP.NET Core 9 Host"]
        direction TB
        MW["Middleware Pipeline<br/>CORS · Auth · Localization · Global Exception"]

        subgraph MODULES["Service Modules"]
            direction LR
            IS["👤 Identity<br/>Service"]
            CS["📚 Content<br/>Service"]
            INS["💬 Interaction<br/>Service"]
            LS["📈 Learning<br/>Service"]
            NS["🔔 Notification<br/>Service"]
            OS["💳 Ordering<br/>Service"]
            SS["🔍 Search<br/>Service"]
        end

        subgraph SHARED["Shared Kernel"]
            direction LR
            DB["AppDbContext"]
            ENT["Entities (ApiResponse, PagedResult)"]
            RES["Resources (i18n RESX)"]
            HUB["NotificationHub"]
            CLD["CloudinaryService"]
        end
    end

    subgraph EXTERNAL["External Services"]
        direction LR
        PG["🐘 PostgreSQL"]
        RD["⚡ Redis (Cache & Hangfire)"]
        CL["☁️ Cloudinary CDN"]
        GM["🤖 Gemini AI"]
        AIS["⚙️ AI Transcribe Server"]
        VP["💰 VnPay Gate"]
        SP["💰 SePay QR"]
        GA["🔐 Google Auth"]
    end

    Client --> MW
    MW --> MODULES
    MODULES --> SHARED
    SHARED --> PG
    SHARED --> RD
    CS --> CL
    SS --> AIS
    SS --> GM
    OS --> VP
    OS --> SP
    IS --> GA

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
    MW->>MW: Global Exception handler
    MW->>MW: CORS Validation
    MW->>MW: JWT Authentication
    MW->>MW: Request Localization (vi/en)
    MW->>CTRL: Route to Controller
    CTRL->>SVC: Call Application Service
    SVC->>REPO: Query/Command
    REPO->>DB: EF Core Query
    DB-->>REPO: Data
    REPO-->>SVC: Domain Entity
    SVC-->>CTRL: DTO / ApiResponse
    CTRL-->>C: ActionResult<ApiResponse>
```

### API Response Format

All endpoints return a consistent `ApiResponse` envelope:

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Thao tác thành công",
  "data": { }
}
```

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Runtime & build tooling |
| [PostgreSQL](https://www.postgresql.org/download/) | 16+ | Primary database |
| [Redis](https://redis.io/download) | alpine / 6+ | Caching, Rate Limiting & Hangfire storage |
| [Docker](https://docs.docker.com/get-docker/) | 20+ | *(Optional)* Containerized deployment |
| [Git](https://git-scm.com/) | 2.30+ | Version control |

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/viettri2004/DACN_BE.git
cd DACN_BE
```

#### 2. Configure Environment Variables

Create a `.env` file in the root directory (or in `src/` directory depending on launch preference):

```bash
# Copy template
cp src/.env.example src/.env
```

Ensure variables are set correctly (see [Environment Configuration](#-environment-configuration) details).

#### 3. Restore Dependencies

```bash
cd src
dotnet restore
```

#### 4. Apply Database Migrations

```bash
dotnet ef database update
```

> [!NOTE]
> Make sure PostgreSQL is running and the connection string is valid in your `.env` before updating database.

#### 5. Run the Project

##### Option A: .NET CLI
```bash
cd src
dotnet run
```
The API is available at:
- **HTTP:** `http://localhost:5223`
- **Swagger UI:** `http://localhost:5223/swagger`
- **Hangfire Dashboard:** `http://localhost:5223/hangfire`

##### Option B: Docker Compose
```bash
docker compose up --build -d
```
This starts:
- **API Server** on port `5223`
- **Redis** on port `6379`
- **Cloudflare Tunnel** container for secure domain access

---

## ⚙️ Environment Configuration

VietEdu uses `.env` files for configuration. The app loads these variables at startup via `DotNetEnv`.

### Required Variables

Ensure `.env` contains the following configurations:

```env
# ─── Database & Cache ────────────────────────────────────────
POSTGRES_CONNECTION="Host=your_postgres_host;Port=5432;Database=vietedu;Username=postgres;Password=your_password;SSL Mode=Prefer;"
REDIS_CONNECTION="localhost:6379"

# ─── Security & JWT ──────────────────────────────────────────
JWT__Issuer="http://localhost:5223"
JWT__Audience="http://localhost:5223"
JWT__SigningKey="your_signing_key_at_least_32_characters"
JWT__AccessTokenExpireMinutes=30

# ─── Cloudinary Media Storage ────────────────────────────────
Cloudinary__Url="cloudinary://API_KEY:API_SECRET@CLOUD_NAME"
CLOUDINARY_URL="cloudinary://API_KEY:API_SECRET@CLOUD_NAME"

# ─── AI Services ─────────────────────────────────────────────
GEMINI_API_KEY="your_google_gemini_api_key"
AI__ServerUrl="http://your-ai-transcribe-server:8000/transcribe-from-cloudinary"

# ─── Email Configuration (SMTP) ──────────────────────────────
Email__Password="your_smtp_password"

# ─── Payment Gateways ────────────────────────────────────────
VnPay__HashSecret="your_vnpay_hash_secret"
Sepay__ApiKey="your_sepay_auth_webhook_token"

# ─── Google OAuth ─────────────────────────────────────────────
Google__ClientId="your_google_client_id"
Google__ClientSecret="your_google_client_secret"

# ─── Cloudflare Tunnel Token ──────────────────────────────────
TUNNEL_TOKEN="your_cloudflare_tunnel_token"
ENABLE_SWAGGER=true
```

---

## 📁 Folder Structure

```
DACN_BE/
├── 📄 DACN_BE.sln                    # Visual Studio solution file
├── 📄 compose.yaml                   # Docker Compose setup
├── 📄 README.md                      # This file
│
├── src/                              # Main API Source Code
│   ├── 📄 Program.cs                 # Application entry point & DI configuration
│   ├── 📄 src.csproj                 # Project file & NuGet packages
│   ├── 📄 Dockerfile                 # Multi-stage Docker deployment config
│   ├── 📄 appsettings.json           # Non-secret application config
│   │
│   ├── Services/                     # 🔷 Service Modules (Clean Architecture)
│   │   ├── IdentityService/          # 👤 Users, Roles, Google OAuth, Dashboards
│   │   │   ├── API/Controllers/      # AccountController, DashboardController
│   │   │   ├── Application/          # DTOs, interfaces, services (Auth, Token, User)
│   │   │   ├── Domain/Entities/      # User, Student, Instructor, RefreshToken
│   │   │   └── Infrastructure/       # Repositories, GoogleAuth, Otp, Email
│   │   │
│   │   ├── ContentService/           # 📚 Course, Lectures, Quizzes, Tags
│   │   │   ├── API/Controllers/      # Course, Lecture, Quiz, Tag, Webhook, Media, InstructorDashboard
│   │   │   ├── Application/          # CourseService, LectureService, QuizService, InstructorService
│   │   │   ├── Domain/Entities/      # Course, Lecture, Quiz, Question, Tag, VideoSubtitle
│   │   │   └── Infrastructure/       # Repositories
│   │   │
│   │   ├── InteractionService/       # 💬 Reviews, Wishlist, Q&A Threads
│   │   │   ├── API/Controllers/      # CommentController, QAController, WishlistController
│   │   │   ├── Application/          # CommentService, QAThreadService, WishlistService
│   │   │   ├── Domain/Entities/      # Comment, Wishlist, QAThread, QAMessage
│   │   │   └── Infrastructure/       # Repositories
│   │   │
│   │   ├── LearningService/          # 📈 Progress tracker & Quiz attempts
│   │   │   ├── API/Controllers/      # StudentProgressController
│   │   │   ├── Application/          # StudentProgressService
│   │   │   ├── Domain/Entities/      # StudentLectureProgress, Enrollment, QuizAttempt, QuizAttemptAnswer
│   │   │   └── Infrastructure/
│   │   │
│   │   ├── NotificationService/      # 🔔 Notification lifecycle
│   │   │   ├── API/Controllers/      # NotificationController
│   │   │   ├── Application/          # NotificationService
│   │   │   ├── Domain/Entities/      # Notification
│   │   │   └── Infrastructure/       # Repositories
│   │   │
│   │   ├── OrderingService/          # 🛒 Carts, Checkout & Payments
│   │   │   ├── API/Controllers/      # CartController, PaymentController
│   │   │   ├── Application/          # PaymentService
│   │   │   ├── Domain/Entities/      # Order, OrderItem, GiftCode, PaymentTransaction
│   │   │   └── Infrastructure/       # SepayService, VnPayService, CartRepository, PaymentRepository
│   │   │
│   │   └── SearchService/            # 🔍 Lucene indexing & AI Processing
│   │       ├── API/Controllers/      # SearchController, AiController
│   │       ├── Application/          # LuceneSearchService
│   │       └── Infrastructure/       # LmsAiService (Gemini API), VideoProcessingService (Hangfire)
│   │
│   ├── Shared/                       # 🔷 Shared Kernel
│   │   ├── Application/
│   │   │   ├── Extension/            # ApiResponseExtension (.ToActionResult() helper)
│   │   │   ├── Interfaces/
│   │   │   └── Middlewares/          # GlobalExceptionMiddleware
│   │   ├── Domain/Entities/          # ApiResponse, PagedResult, SharedResources
│   │   ├── Infrastructure/
│   │   │   ├── AppDbContext.cs       # Shared DbContext
│   │   │   ├── DbInitializer.cs      # Database seed data configuration
│   │   │   ├── Hubs/                 # SignalR Hubs (NotificationHub)
│   │   │   ├── Cloudiary/            # Cloudinary upload integrations
│   │   │   └── Repositories/
│   │   └── Resources/                # Localization resource files (.resx)
│   │
│   └── Migrations/                   # EF Core DB migrations
│
└── tests/                            # Unit & Integration Tests
    └── Services/
        ├── ContentService.Tests/
        ├── IdentityService.Tests/
        ├── InteractionService.Tests/
        ├── LearningService.Tests/
        ├── NotificationService.Tests/
        ├── OrderingService.Tests/
        └── SearchService.Tests/
```

---

## 🔌 API Reference

### Base URL

```
http://localhost:5223/api
```

### Authentication

Protected endpoints require a JWT Bearer token:

```
Authorization: Bearer <your_jwt_token>
```

### Localization

Control responses language via `Accept-Language` header:

```
Accept-Language: vi    # Vietnamese (default)
Accept-Language: en    # English
```

### Available Endpoints

| Service Module | Endpoint | Methods | Description |
|---|---|---|---|
| **Identity** | `/api/Account` | `GET` `POST` `PUT` `DELETE` | Login, registration, profile updates |
| **Identity** | `/api/Account/google-*` | `GET` `POST` | Google OAuth login and callback redirection |
| **Identity** | `/api/Dashboard` | `GET` | System-wide admin dashboard analytics |
| **Content** | `/api/Course` | `GET` `POST` `PUT` `DELETE` | Course catalog, publishing approvals, recommendations |
| **Content** | `/api/InstructorDashboard`| `GET` | Course performance statistics for instructors |
| **Content** | `/api/Lecture` | `GET` `POST` `PUT` `DELETE` | Lecture items (videos, document downloads) |
| **Content** | `/api/Quiz` | `GET` `POST` `PUT` `DELETE` | Quizzes management, questions structure |
| **Content** | `/api/Tag` | `GET` `POST` `PUT` `DELETE` | Manage categories/tags |
| **Content** | `/api/Media/*` | `GET` | Signature generators for Cloudinary upload validation |
| **Content** | `/api/webhooks/cloudinary`| `POST` | Notification webhook triggered by Cloudinary processing |
| **Interaction**| `/api/Comment` | `GET` `POST` `DELETE` | Course comments and review posts |
| **Interaction**| `/api/QA` | `GET` `POST` `PUT` `DELETE` | Lecture discussion threads and Q&A messages |
| **Interaction**| `/api/Wishlist` | `GET` `POST` `DELETE` | Student course wishlist operations |
| **Learning** | `/api/StudentProgress` | `GET` `POST` | Course progress tracking & lecture completion |
| **Notification**| `/api/Notification` | `GET` `POST` `PUT` | Push notification management |
| **Ordering** | `/api/Cart` | `GET` `POST` `DELETE` | Shopping cart updates |
| **Ordering** | `/api/Payment/*` | `POST` `GET` | Checkout and IPN handlers for VnPay and SePay |
| **Search** | `/api/Search/index-all` | `POST` | Command for re-indexing Lucene databases (Admin) |
| **Search** | `/api/Ai/analyze` | `POST` | Cloudinary video analysis summary with Google Gemini |

---

## 🧪 Tech Stack

- **Runtime** — .NET 9.0 (ASP.NET Core API Host)
- **Language** — C# 13
- **Primary Database** — PostgreSQL via EF Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Caching & Queueing** — Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`)
- **Background Jobs** — Hangfire with Redis Storage (`Hangfire.Redis.StackExchange`)
- **Authentication** — ASP.NET Identity + JWT Bearer + Google Authentication
- **Search System** — Apache Lucene.NET 4.8 (Whitespace tokenizer, spellchecking, facets)
- **AI Integrations** — Google Gemini API (`Google.GenAI` SDK 0.1.0)
- **Payment APIs** — VnPay Integration & SePay Webhooks (with QR code generation via `QRCoder`)
- **Real-Time Delivery** — ASP.NET Core SignalR
- **Media CDN** — Cloudinary (`CloudinaryDotNet`)
- **Logging** — Serilog (Console and daily rolling file logs)
- **API Documentation** — Swashbuckle (Swagger/OpenAPI UI integration)
- **Mapping** — AutoMapper 13.0.1
- **Deployment** — Docker multi-stage builds & Docker Compose

---

## 🤝 Contributing

We welcome contributions! Here's how you can help make VietEdu better.

### Commit Conventions

We follow [Conventional Commits](https://www.conventionalcommits.org/):

| Prefix | Purpose | Example |
|---|---|---|
| `feat` | New feature | `feat(ordering): add SePay QR code generator` |
| `fix` | Bug fix | `fix(search): repair Lucene index lock exceptions` |
| `docs` | Documentation | `docs: update API endpoints list in README` |
| `refactor`| Code cleanup | `refactor(shared): optimize dbContext pool limits` |
| `test` | Add tests | `test(content): write tests for Quiz lifecycle` |
| `chore` | Maintenance | `chore: update Swashbuckle dependencies` |

### Coding Guidelines

- **Controllers** — Keep them slim. Return `ActionResult<ApiResponse>` and invoke the `.ToActionResult()` helper extensions.
- **Dependency Injection** — Add registrations inside the `ConfigureDI` method in `Program.cs`.
- **Localization** — Maintain string resource keys inside `SharedResources.resx`. Avoid hardcoded response strings.
- **Tests** — Add unit and integration tests under the relevant service project in `tests/Services/`.

---

## 📜 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Built with ❤️ by the <strong>VietEdu</strong> team
</p>
