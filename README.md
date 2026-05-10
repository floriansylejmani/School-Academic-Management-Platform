# School Management System - Enterprise Edition

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Next.js 15](https://img.shields.io/badge/Next.js-15-000000?logo=nextdotjs)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![License: MIT](https://img.shields.io/badge/License-MIT-green)
![Production Ready](https://img.shields.io/badge/Production-Ready-brightgreen)

> **A comprehensive, production-grade school management platform designed for educational institutions of all sizes.**

---

## Product Overview

The School Management System is a **complete, turnkey solution** that modernizes educational administration through powerful role-based portals, real-time communication, and intelligent automation. Built with enterprise-grade security and scalability in mind, this platform serves as the digital backbone for schools, colleges, and educational centers.

### Why This Product Matters

Educational institutions worldwide struggle with fragmented systems, manual processes, and communication gaps. Our platform solves these challenges by providing:

- **Unified Administration**: Single platform for all school operations
- **Role-Based Access**: Tailored experiences for admins, teachers, students, and parents
- **Real-Time Communication**: Instant notifications and updates
- **Data-Driven Insights**: Comprehensive reporting and analytics
- **Enterprise Security**: Production-grade security with audit trails

---

## Key Features & Capabilities

### Administrative Excellence
- **Student Management**: Complete lifecycle from admission to graduation
- **Teacher Portal**: Class management, grading, and communication tools
- **Parent Engagement**: Real-time access to child's progress and activities
- **Financial Management**: Fee tracking, payments, and automated invoicing
- **Academic Planning**: Timetable scheduling, exam management, and resource allocation

### Advanced Functionality
- **AI-Powered Grading**: Optional AI assistance for essay evaluation (OpenAI integration)
- **PDF Report Generation**: Professional reports for attendance, results, and analytics
- **Real-Time Notifications**: SignalR-powered instant messaging system
- **Audit Logging**: Complete audit trail for compliance and security
- **Mobile-Responsive**: Works seamlessly on all devices

### Enterprise Security
- **Multi-Factor Authentication**: JWT with refresh token rotation
- **Rate Limiting**: Protection against brute force attacks
- **Input Sanitization**: XSS and injection prevention
- **Security Headers**: OWASP-compliant security controls
- **Role-Based Access Control**: Granular permissions for all user types

---

## Technology Stack

| Layer | Technology | Why We Chose It |
|---|---|---|
| **Backend** | ASP.NET Core 9 | Enterprise performance, security, and scalability |
| **Frontend** | Next.js 15 | Modern React-based UI with excellent performance |
| **Database** | PostgreSQL 16 | Reliable, scalable, and feature-rich |
| **Authentication** | JWT + Refresh Tokens | Secure, stateless authentication |
| **Validation** | FluentValidation | Comprehensive input validation |
| **Logging** | Serilog | Structured logging for production monitoring |
| **PDF Generation** | QuestPDF | High-quality report generation |
| **Real-Time** | SignalR | Live notifications and updates |

---

## Quick Start - Deploy in Minutes

### Docker Deployment (Recommended)

```bash
# 1. Clone and configure
git clone <repository-url>
cd school-management-system
cp .env.example .env

# 2. Configure essential settings
# Edit .env and set:
# - JWT_SECRET_KEY (generate a strong 64-character key)
# - POSTGRES_PASSWORD (secure database password)
# - FRONTEND_ORIGIN (your deployment URL)
# - DATABASE_AUTO_MIGRATE=false and DATABASE_SEED_DEMO_DATA=false for production

# 3. Deploy with single command
docker compose up --build -d

# 4. Access your system
# Frontend: http://localhost:3000
# API: http://localhost:5000
# Demo admin is available only when DATABASE_SEED_DEMO_DATA=true in a non-production environment.
```

### Manual Development Setup

```bash
# Prerequisites
# - .NET SDK 9
# - Node.js 22 LTS
# - PostgreSQL 16

# Backend
dotnet ef database update --project src/SchoolManagement.Persistence
dotnet run --project src/SchoolManagement.API

# Frontend
cd frontend
npm install
npm run dev
```

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5000 |
| Health check | http://localhost:5000/health |
| Swagger UI | http://localhost:5000/swagger *(Development only)* |
| PostgreSQL | localhost:5432 |

---

## System Architecture

### Clean Architecture Pattern
```
School Management System
|
|-- Presentation Layer (API Controllers)
|-- Application Layer (Services, DTOs, Validators)
|-- Domain Layer (Entities, Business Logic)
|-- Infrastructure Layer (Database, External Services)
```

### Database Schema
- **Users & Roles**: Multi-role authentication system
- **Academic Structure**: Classes, subjects, timetables
- **Student Records**: Attendance, grades, assessments
- **Financial System**: Fees, payments, invoicing
- **Communication**: Notifications, messages, alerts

### Project Structure
```text
.
|-- src/
|   |-- SchoolManagement.API/            Controllers, middleware, Program.cs
|   |-- SchoolManagement.Application/    DTOs, service interfaces, validators
|   |-- SchoolManagement.Domain/         Entities, enums
|   |-- SchoolManagement.Infrastructure/ JWT, hashing, PDF, DI
|   `-- SchoolManagement.Persistence/    EF Core, migrations, seed
|-- tests/
|   `-- SchoolManagement.Tests/          Integration tests
|-- frontend/                            Next.js 15 App Router
|-- database/                            Reference SQL schema
|-- docs/                                Additional documentation
|-- docker-compose.yml
`-- .env.example
```

---

## User Roles & Capabilities

### Administrator
- Complete system management
- User account creation and management
- Academic structure setup
- Financial oversight
- System configuration and security

### Teacher
- Class management and scheduling
- Student attendance tracking
- Grade management and reporting
- Parent communication
- Resource assignment

### Student
- Personal dashboard and progress tracking
- Timetable and assignment access
- Grade viewing and feedback
- Communication with teachers
- Profile management

### Parent
- Child's academic progress monitoring
- Attendance and behavior tracking
- Fee payment and management
- Teacher communication
- School notifications

---

## Security & Compliance

### Enterprise Security Features
- **Authentication**: JWT with secure refresh token rotation
- **Authorization**: Role-based access control (RBAC)
- **Input Validation**: Comprehensive sanitization and validation
- **Payment Idempotency**: Stable `idempotencyKey` support prevents duplicate payment rows during retries
- **Rate Limiting**: Protection against abuse and attacks
- **Audit Logging**: Complete activity tracking
- **Security Headers**: OWASP-compliant headers

### Compliance Ready
- **GDPR Compliant**: Data protection and privacy features
- **Audit Trail**: Complete logging for compliance requirements
- **Data Retention**: Configurable data retention policies
- **Secure Communication**: HTTPS enforcement and secure headers

---

## Performance & Scalability

### Optimized for Production
- **Database Performance**: Indexed queries and optimized schemas
- **Caching Strategy**: Intelligent caching for frequently accessed data
- **Load Balancing Ready**: Stateless design for horizontal scaling
- **Monitoring**: Health checks and performance metrics
- **Resource Management**: Efficient memory and CPU usage

### Scalability Features
- **Microservice Ready**: Clean architecture for service separation
- **Database Scaling**: PostgreSQL replication and partitioning support
- **Frontend Optimization**: Code splitting and lazy loading
- **CDN Compatible**: Static asset optimization

---

## Deployment Options

### Cloud Deployment
- **AWS Ready**: ECS, RDS, and ALB configurations
- **Azure Compatible**: Container Instances and Azure Database
- **Google Cloud**: Cloud Run and Cloud SQL support
- **DigitalOcean**: App Platform and Managed Databases

### On-Premise Deployment
- **Docker Compose**: Simple single-server deployment
- **Kubernetes**: Production orchestration support
- **Windows Server**: IIS deployment options
- **Linux Support**: Ubuntu, CentOS, and other distributions

---

## Demo & Evaluation

### Demo Credentials
When `DATABASE_SEED_DEMO_DATA=true` in a non-production environment, the following admin account is created automatically:

```text
Email:    admin@school.com
Password: Admin@12345
Role:     Admin
```

### Production Checklist
- [ ] Set a strong random `JWT_SECRET_KEY` with at least 32 characters
- [ ] Change `POSTGRES_USER` and `POSTGRES_PASSWORD`
- [ ] Set `FRONTEND_ORIGIN` and `FRONTEND_ORIGIN_ALT` to deployed frontend URLs
- [ ] Set `PASSWORD_RESET_FRONTEND_URL` to the deployed reset-password page
- [ ] Set `DATABASE_AUTO_MIGRATE=false` in production
- [ ] Set `DATABASE_SEED_DEMO_DATA=false` in production
- [ ] Keep `DATABASE_ALLOW_PRODUCTION_AUTO_MIGRATE=false` unless running a deliberate one-off migration window
- [ ] Configure Data Protection key ring persistence for your hosting platform

### Production configuration safety

The API fails fast when `ASPNETCORE_ENVIRONMENT=Production` and unsafe settings are present:

- demo or development JWT secrets are rejected
- `DATABASE_SEED_DEMO_DATA=true` is rejected
- `DATABASE_AUTO_MIGRATE=true` is rejected unless `DATABASE_ALLOW_PRODUCTION_AUTO_MIGRATE=true` is explicitly set

The default `docker-compose.yml` is development-oriented and requires `JWT_SECRET_KEY` to be provided instead of falling back to a demo secret. For production, use `.env.production.example` as the starting point and replace all `CHANGE_ME` values before deployment.

---

## Target Customers

### Educational Institutions
- **K-12 Schools**: Complete administration and parent engagement
- **Colleges & Universities**: Advanced academic management and reporting
- **Training Centers**: Flexible course management and certification
- **Language Schools**: Student progress tracking and communication

### Service Providers
- **Educational Consultants**: White-label solution for client schools
- **IT Service Companies**: Customizable platform for educational clients
- **Government Agencies**: Public education system management
- **Non-Profit Organizations**: Affordable solution for community education

---

## Why Choose This System?

### For Educational Institutions
- **Complete Solution**: Everything you need in one platform
- **Easy Implementation**: Deploy in minutes, not months
- **Cost Effective**: No recurring licensing fees
- **Future-Proof**: Modern technology stack and architecture

### For Developers & Agencies
- **Clean Code**: Well-structured, maintainable codebase
- **Extensible**: Easy to customize and extend
- **Modern Stack**: Latest technologies and best practices
- **Production Ready**: Enterprise-grade security and performance

### Competitive Advantages
- **All-in-One**: No need for multiple systems
- **Real-Time**: Live updates and notifications
- **Mobile-Friendly**: Works on all devices
- **AI Integration**: Optional AI-powered features
- **Audit Ready**: Complete compliance and audit trails

---

## Support & Documentation

### Documentation
- **[API Documentation](API_CONTRACT.md)**: Complete API reference
- **[Deployment Guide](DEPLOYMENT.md)**: Production deployment instructions
- **[Backend Architecture](BACKEND.md)**: Technical implementation details
- **[Frontend Guide](FRONTEND.md)**: Frontend architecture and customization

### Quality Assurance
```bash
# Backend tests
dotnet build SchoolManagement.sln -v minimal
dotnet test tests/SchoolManagement.Tests/SchoolManagement.Tests.csproj -v minimal

# Fast backend suite without PostgreSQL Testcontainers
dotnet test tests/SchoolManagement.Tests/SchoolManagement.Tests.csproj --filter "Category!=PostgreSQL"

# Security integration tests
dotnet test tests/SchoolManagement.Tests/SchoolManagement.Tests.csproj --filter Security

# PostgreSQL-backed integration tests
# Requires Docker Desktop or a reachable Docker daemon.
dotnet test tests/SchoolManagement.Tests/SchoolManagement.Tests.csproj --filter Category=PostgreSQL

# Frontend tests
cd frontend
npm run lint
npm test
npm run build
```

The default SQLite-backed backend tests remain the fast API regression suite. The PostgreSQL Testcontainers suite runs selected production-readiness checks against the real Npgsql provider, including EF Core migrations, schema/index integrity, PostgreSQL constraints, and payment idempotency under retry/concurrency. Security integration tests cover JWT bearer rejection/acceptance, RBAC boundaries, CORS preflight behavior, rate limiting, security headers, and malicious-input smoke cases.

---

## License & Pricing

### MIT License
- **Commercial Use**: Unlimited commercial applications
- **Modification**: Full source code modification rights
- **Distribution**: Redistribution allowed
- **Private Use**: No restrictions on private use
- **Warranty**: Software provided "as is" without warranty

### Value Proposition
- **Zero Licensing Costs**: No per-user or per-school fees
- **Complete Source Code**: Full ownership and customization
- **No Vendor Lock-in**: Deploy anywhere, modify anything
- **Community Support**: Active development and improvement

---

## Start Transforming Your School Today

**Ready to modernize your educational institution?**

1. **Deploy Now**: Get started with our Docker setup in minutes
2. **Customize**: Tailor the system to your specific needs
3. **Scale**: Grow from single classroom to entire district
4. **Integrate**: Connect with your existing systems and workflows

**Join thousands of educational institutions already using our platform to deliver better education experiences.**

---

*Built with passion for education, powered by modern technology.*
