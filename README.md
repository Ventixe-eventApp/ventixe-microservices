# 🎫 Ventixe - Event Ticketing & AI-Powered Booking Platform

> A modern, cloud-native event discovery and booking platform with AI-powered recommendations and semantic search capabilities.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.1.0-blue)](https://react.dev)
[![Docker](https://img.shields.io/badge/Docker-Containerized-blue)](https://www.docker.com/)

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Quick Start](#quick-start)
- [Services](#services)
- [API Documentation](#api-documentation)
- [Environment Configuration](#environment-configuration)
- [Development Guide](#development-guide)
- [Database Setup](#database-setup)
- [Docker & Deployment](#docker--deployment)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

Ventixe is a comprehensive event ticketing platform that combines:

- **Event Discovery**: Browse and search events across multiple categories
- **Intelligent Booking**: Seamless event reservation and ticket management
- **AI-Powered Features**: 
  - Semantic search using Google AI embeddings
  - Personalized event recommendations
  - Vector-based similarity search with Qdrant
- **Multi-Tenant Architecture**: Complete microservices-based backend
- **Modern Frontend**: React + Vite for optimal performance

### Key Features

✅ **Microservices Architecture** - Independent, scalable services  
✅ **AI/ML Integration** - Semantic search and smart recommendations  
✅ **User Authentication** - Secure JWT-based authentication  
✅ **Event Management** - Full booking lifecycle  
✅ **Email Verification** - OTP-based verification system  
✅ **Cloud-Ready** - Containerized and deployable on Azure  
✅ **RESTful APIs** - OpenAPI/Swagger documentation included  

---

## 🏗️ Architecture

### System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      React Frontend (Vite)                      │
│                     Port 5173 / Azure Static                     │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP/HTTPS
         ┌───────────────┴───────────────┐
         │                               │
    ┌────┴─────┐      ┌─────────────────────────────────────┐
    │   API    │      │      Microservices Backend           │
    │ Gateway  │      │  (ASP.NET Core 9.0, C#)             │
    └────┬─────┘      │                                     │
         │            │  ┌─────────────────────────────┐   │
         │            │  │  Authentication Layer       │   │
         │            │  │  - JWT/OAuth               │   │
         │            │  │  - Identity Management     │   │
         │            │  └─────────────────────────────┘   │
         │            │            │                       │
         │            │  ┌─────────┴──────────────────┐   │
         │            │  │  Service Layer             │   │
         │            │  ├─ Account Service (5001)    │   │
         │            │  ├─ Auth Service (7001)       │   │
         │            │  ├─ Booking Service (5003)    │   │
         │            │  ├─ Event Service (5005)      │   │
         │            │  ├─ User Service (5007)       │   │
         │            │  ├─ Verification Service (5009)│  │
         │            │  └─ AI Service (5011)         │   │
         │            │            │                  │   │
         │            └────────────┬──────────────────┘   │
         │                         │                      │
    ┌────┴─────────────────────────┴───────────────────┐
    │       Data & Vector Layer                        │
    ├─ SQL Server 2022 (6 databases)                  │
    │  - AccountDb, AuthDb, BookingDb, EventDb,       │
    │    UserDb, VerifyDb                             │
    ├─ Qdrant Vector Database (Port 6333/6334)        │
    │  - AI embeddings for semantic search            │
    └─────────────────────────────────────────────────┘
```

### Service Topology

| Service | Port (HTTP/HTTPS) | Database | Purpose |
|---------|-------------------|----------|---------|
| **Account Service** | 5001:8080 / 5002:8081 | AccountDb | Account management & identity |
| **Auth Service** | 7001:8080 / 7002:8081 | AuthDb | Authentication & authorization |
| **Booking Service** | 5003:8080 / 5004:8081 | BookingDb | Event bookings & reservations |
| **Event Service** | 5005:8080 / 5006:8081 | EventDb | Event data & management |
| **User Service** | 5007:8080 / 5008:8081 | UserDb | User profiles & preferences |
| **Verification Service** | 5009:8080 / 5010:8081 | VerifyDb | Email/SMS verification |
| **AI Service** | 5011:8080 | EventDb | Recommendations & semantic search |
| **Frontend** | 5173 | N/A | React web application |

---

## 🛠️ Tech Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Language**: C#
- **ORM**: Entity Framework Core 9.0.5
- **Database**: Microsoft SQL Server 2022
- **Authentication**: ASP.NET Core Identity with JWT
- **API Documentation**: Swagger/OpenAPI 3.0
- **Containerization**: Docker
- **Build Tool**: dotnet CLI

### AI/ML Services
- **Microsoft Semantic Kernel** v1.68.0 - AI orchestration
- **Google AI Embeddings** - Semantic text embeddings
- **Qdrant** - Vector database for similarity search
- **Microsoft.Extensions.AI** - AI abstractions

### Frontend
- **Framework**: React 19.1.0
- **Build Tool**: Vite 6.3.5
- **Routing**: React Router DOM 7.6.0
- **Form Management**: React Hook Form 7.56.4
- **Linting**: ESLint
- **Package Manager**: npm

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Orchestration**: Docker Compose (local), Kubernetes-ready
- **Cloud Platform**: Azure (Web Apps, Static Web Apps)
- **CI/CD**: GitHub Actions
- **Version Control**: Git

---

## 📂 Project Structure

```
Ventixe-All/
│
├── Backend Services (C# / ASP.NET Core 9.0)
│   ├── account-service/                  # Account & identity management
│   ├── auth-service/                     # Authentication & JWT tokens
│   ├── booking-service/                  # Event bookings & reservations
│   ├── event-service/                    # Event listings & data
│   ├── user-service/                     # User profiles & preferences
│   ├── verification-provider-service/    # Email/SMS verification
│   └── Ventixe.AI.Service/               # AI recommendations & search
│
├── Frontend
│   └── ventixereact/                     # React + Vite web application
│
├── Infrastructure & Configuration
│   ├── docker-compose.yml                # Full stack orchestration
│   ├── docker-compose.override.yml       # Local development overrides
│   ├── Ventixe.sln                       # Visual Studio solution
│   ├── launchSettings.json               # VS launch configuration
│   └── .env / .env.example               # Environment variables
│
├── .github/
│   └── workflows/                        # GitHub Actions CI/CD
│
├── qdrant_data/                          # Persistent vector DB storage
├── bin/ & obj/                           # Build artifacts
└── README.md                             # This file

### Individual Service Structure
{service-name}/
├── Presentation/
│   ├── Controllers/                      # REST API endpoints
│   ├── Models/                           # DTOs & request/response models
│   └── Service/                          # Business logic layer
├── Data/                                 # Data access layer
├── Migrations/                           # EF Core database migrations
├── Program.cs                            # Service entry point
├── Dockerfile                            # Container image definition
├── {Service}.csproj                      # Project configuration
└── README.md                             # Service-specific docs
```

---

## 🚀 Quick Start

### Prerequisites

- **Docker & Docker Compose** (v20.10+)
- **.NET 9.0 SDK** (for local development without Docker)
- **Node.js** 18+ (for React development)
- **Git**

### Running with Docker (Recommended)

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/Ventixe-All.git
   cd Ventixe-All
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your configuration:
   # - SQL_PASSWORD: SQL Server admin password
   # - GOOGLE_API_KEY: For semantic embeddings
   # - CERT_PASSWORD: For HTTPS certificates
   ```

3. **Start all services**
   ```bash
   docker-compose up --build
   ```

   This will start:
   - SQL Server 2022 (port 1433)
   - Qdrant vector database (port 6333, 6334)
   - 6 Backend microservices (ports 5001-5011)
   - React frontend (port 5173)

4. **Verify services are running**
   ```bash
   # Check running containers
   docker ps

   # View logs
   docker-compose logs -f
   ```

5. **Access the application**
   - **Frontend**: http://localhost:5173
   - **API Gateway/Event Service**: http://localhost:5005/swagger
   - **Auth Service**: http://localhost:7001/swagger
   - **Qdrant Dashboard**: http://localhost:6333/dashboard

### Running Locally (Without Docker)

1. **Setup SQL Server**
   ```bash
   # Install SQL Server 2022 locally
   # Create databases: AccountDb, AuthDb, BookingDb, EventDb, UserDb, VerifyDb
   ```

2. **Run migrations for each service**
   ```bash
   cd account-service
   dotnet ef database update
   cd ../auth-service
   dotnet ef database update
   # ... repeat for other services
   ```

3. **Start each service**
   ```bash
   cd account-service
   dotnet run

   # In separate terminals:
   cd auth-service && dotnet run
   cd booking-service && dotnet run
   # ... etc
   ```

4. **Start the frontend**
   ```bash
   cd ventixereact
   npm install
   npm run dev
   ```

---

## 📡 Services

### Account Service (Port 5001)
Manages user accounts, identity, and account-related operations.

**Key Endpoints:**
- `POST /api/accounts/register` - Create new account
- `GET /api/accounts/{id}` - Get account details
- `PUT /api/accounts/{id}` - Update account
- `DELETE /api/accounts/{id}` - Delete account

### Auth Service (Port 7001)
Handles authentication, JWT token generation, and authorization.

**Key Endpoints:**
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/validate` - Validate token

### Booking Service (Port 5003)
Manages event bookings, reservations, and ticket operations.

**Key Endpoints:**
- `POST /api/bookings` - Create new booking
- `GET /api/bookings/{id}` - Get booking details
- `GET /api/bookings/user/{userId}` - List user bookings
- `PUT /api/bookings/{id}/cancel` - Cancel booking
- `GET /api/bookings/{id}/tickets` - Get booking tickets

### Event Service (Port 5005)
Manages event listings, details, categories, and metadata.

**Key Endpoints:**
- `GET /api/events` - List all events
- `GET /api/events/{id}` - Get event details
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event
- `GET /api/events/search` - Search events

### User Service (Port 5007)
Manages user profiles, preferences, and user data.

**Key Endpoints:**
- `GET /api/users/{id}` - Get user profile
- `PUT /api/users/{id}` - Update profile
- `GET /api/users/{id}/preferences` - Get user preferences
- `PUT /api/users/{id}/preferences` - Update preferences

### Verification Service (Port 5009)
Handles email/SMS verification, OTP generation, and validation.

**Key Endpoints:**
- `POST /api/verification/send` - Send verification code
- `POST /api/verification/validate` - Validate verification code
- `GET /api/verification/status/{id}` - Check verification status

### AI Service (Port 5011)
Provides AI-powered features: semantic search, recommendations, and embeddings.

**Key Endpoints:**
- `POST /api/ai/index` - Index events and generate embeddings
- `POST /api/ai/search` - Semantic search for events
- `GET /api/ai/recommendations/{userId}` - Get personalized recommendations
- `POST /api/ai/embeddings` - Generate text embeddings

---

## 📚 API Documentation

Each service includes **Swagger/OpenAPI** documentation. After starting the services, access them at:

- Account Service: http://localhost:5001/swagger
- Auth Service: http://localhost:7001/swagger
- Booking Service: http://localhost:5003/swagger
- Event Service: http://localhost:5005/swagger
- User Service: http://localhost:5007/swagger
- Verification Service: http://localhost:5009/swagger
- AI Service: http://localhost:5011/swagger

### API Authentication

All protected endpoints require a JWT token in the `Authorization` header:

```bash
curl -H "Authorization: Bearer {JWT_TOKEN}" \
     http://localhost:5005/api/events
```

**Obtaining a JWT Token:**

1. Register/Login via Auth Service:
   ```bash
   curl -X POST http://localhost:7001/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username": "user@example.com", "password": "password"}'
   ```

2. Use the returned `access_token` in subsequent requests.

---

## ⚙️ Environment Configuration

### Required Environment Variables (.env)

```env
# Database
SQL_SERVER=sqlserver
SQL_USER=sa
SQL_PASSWORD=BytMig123!
CONNECTION_STRING=Server=sqlserver;User Id=sa;Password=BytMig123!;TrustServerCertificate=True;

# AI/ML Services
GOOGLE_API_KEY=AIzaSyC3_jZER6xmIef3nyDeeUC-FEReGw4R3jI

# Security
CERT_PASSWORD=NewPassword123!
JWT_SECRET=your_jwt_secret_key_here_min_32_chars_long

# Service URLs
ACCOUNT_SERVICE_URL=http://account-service:8080
AUTH_SERVICE_URL=http://auth-service:8080
BOOKING_SERVICE_URL=http://booking-service:8080
EVENT_SERVICE_URL=http://event-service:8080
USER_SERVICE_URL=http://user-service:8080
VERIFICATION_SERVICE_URL=http://verification-service:8080

# Qdrant
QDRANT_URL=http://qdrant:6333

# Frontend
REACT_APP_API_URL=http://localhost:5005
REACT_APP_AUTH_SERVICE_URL=http://localhost:7001
```

### Creating .env File

```bash
# Copy the template
cp .env.example .env

# Edit with your configuration
# On Windows
notepad .env

# On Linux/Mac
nano .env
```

---

## 💻 Development Guide

### Prerequisites for Local Development

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server 2022 Developer Edition
- Node.js 18+
- Git

### Setting Up Development Environment

1. **Clone repository**
   ```bash
   git clone https://github.com/your-org/Ventixe-All.git
   cd Ventixe-All
   ```

2. **Setup SQL Server**
   ```bash
   # Download and install SQL Server 2022 Developer Edition
   # Create databases via SQL Server Management Studio (SSMS)
   ```

3. **Install dependencies**

   For backend services:
   ```bash
   # Dependencies are managed in .csproj files
   # .NET will auto-restore on first build/run
   ```

   For frontend:
   ```bash
   cd ventixereact
   npm install
   ```

4. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with local SQL Server connection details
   ```

5. **Run database migrations**
   ```bash
   cd account-service
   dotnet ef database update --configuration Debug

   cd ../auth-service
   dotnet ef database update --configuration Debug
   # ... repeat for other services
   ```

6. **Start development servers**

   **Backend** (run in separate terminals):
   ```bash
   # Terminal 1: Account Service
   cd account-service
   dotnet watch run

   # Terminal 2: Auth Service
   cd auth-service
   dotnet watch run

   # Terminal 3: Event Service
   cd event-service
   dotnet watch run

   # ... etc for other services
   ```

   **Frontend** (Vite dev server with HMR):
   ```bash
   cd ventixereact
   npm run dev
   ```

### Code Organization Standards

- **Controllers**: REST endpoint definitions
- **Models**: Data Transfer Objects (DTOs)
- **Services**: Business logic and processing
- **Data**: Repository pattern for database access
- **Migrations**: EF Core database schema changes

### Running Tests

```bash
# Run unit tests for a specific service
cd event-service
dotnet test

# Run all tests
dotnet test *.sln
```

### Code Style & Linting

**Backend (C#):**
```bash
# Code analysis
dotnet analyze

# Style violations
dotnet format --verify-no-changes
```

**Frontend (JavaScript/React):**
```bash
cd ventixereact
npm run lint
npm run lint:fix
```

---

## 🗄️ Database Setup

### Database Architecture

Single SQL Server instance with 6 separate databases:

| Database | Purpose | Tables |
|----------|---------|--------|
| **AccountDb** | User accounts & credentials | Accounts, Users, Roles |
| **AuthDb** | Authentication & sessions | Tokens, Sessions, RefreshTokens |
| **BookingDb** | Event reservations | Bookings, Tickets, Payments |
| **EventDb** | Event data & catalog | Events, Categories, Venues |
| **UserDb** | User profiles & preferences | Profiles, Preferences, Settings |
| **VerifyDb** | Verification records | VerificationCodes, Attempts |

### Database Initialization

**Using Docker Compose:**
```bash
docker-compose up
# Databases auto-create from migrations
```

**Using SQL Server Management Studio (SSMS):**
```sql
-- Create databases
CREATE DATABASE AccountDb;
CREATE DATABASE AuthDb;
CREATE DATABASE BookingDb;
CREATE DATABASE EventDb;
CREATE DATABASE UserDb;
CREATE DATABASE VerifyDb;
```

### Running Migrations

```bash
# For a specific service
cd event-service
dotnet ef database update

# View pending migrations
dotnet ef migrations list

# Add a new migration
dotnet ef migrations add AddEventFeature

# Rollback migration
dotnet ef database update PreviousMigration
```

### Connection String Format

```
Server=localhost;Database=DatabaseName;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

### Backing Up Databases

```bash
# Backup via Docker
docker exec ventixe-sqlserver sqlcmd -S localhost -U sa -P $SQL_PASSWORD -Q "BACKUP DATABASE [EventDb] TO DISK='/var/opt/mssql/backup/EventDb.bak'"

# Restore
docker exec ventixe-sqlserver sqlcmd -S localhost -U sa -P $SQL_PASSWORD -Q "RESTORE DATABASE [EventDb] FROM DISK='/var/opt/mssql/backup/EventDb.bak'"
```

---

## 🐳 Docker & Deployment

### Docker Compose Services

```yaml
Services:
  - sqlserver        : SQL Server 2022 (port 1433)
  - qdrant           : Vector database (ports 6333/6334)
  - account-service  : Account microservice (port 5001)
  - auth-service     : Auth microservice (port 7001)
  - booking-service  : Booking microservice (port 5003)
  - event-service    : Event microservice (port 5005)
  - user-service     : User microservice (port 5007)
  - verification-service: Verification microservice (port 5009)
  - ai-service       : AI microservice (port 5011)
  - ventixe-react    : Frontend (port 5173)
```

### Building Docker Images

```bash
# Build specific service
docker build -t ventixe/account-service:latest ./account-service

# Build all services via compose
docker-compose build

# Build with no cache
docker-compose build --no-cache
```

### Running Containers

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d account-service

# View logs
docker-compose logs -f event-service

# Stop services
docker-compose down

# Remove volumes
docker-compose down -v
```

### Production Deployment (Azure)

**Backend Services to Azure App Service:**
```bash
# Configure Azure CLI
az login
az account set --subscription "Your Subscription"

# Create App Service
az appservice plan create -g ventixe-prod -n ventixe-plan --sku B2
az webapp create -g ventixe-prod -p ventixe-plan -n ventixe-event-service

# Deploy using GitHub Actions (see .github/workflows/)
# Automatically triggered on merge to master
```

**Frontend to Azure Static Web Apps:**
```bash
# Configuration in GitHub Actions
# Automatically deployed on merge to main
# Output: dist/ directory
```

### Environment-Specific Configuration

**Development (.env.development)**
- SQL Server: Local or Docker container
- Debug logging enabled
- CORS: Allow all origins
- AI features: Limited models

**Production (.env.production)**
- SQL Server: Azure SQL Database
- Structured logging
- CORS: Restricted to frontend domain
- AI features: Full models
- SSL/TLS required

---

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

### Getting Started

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. **Make your changes**
4. **Run tests locally**
   ```bash
   dotnet test
   cd ventixereact && npm run lint && npm run build
   ```
5. **Commit with clear messages**
   ```bash
   git commit -m "feat: Add amazing feature"
   ```
6. **Push to your fork**
   ```bash
   git push origin feature/amazing-feature
   ```
7. **Create a Pull Request**

### Code Standards

- Follow **C# naming conventions** (PascalCase for public members)
- Follow **JavaScript/React best practices** (ESLint configured)
- Write **unit tests** for new features
- Add **comments** for complex logic
- Keep commits **atomic and focused**

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `docs`: Documentation changes
- `chore`: Maintenance tasks

**Examples:**
```
feat(events): Add event filtering by category
fix(auth): Resolve JWT expiration issue
docs(readme): Update database setup instructions
```

---

## 🐛 Troubleshooting

### Common Issues & Solutions

#### Issue: Docker containers won't start
**Solution:**
```bash
# Clean up containers and volumes
docker-compose down -v

# Rebuild from scratch
docker-compose up --build --force-recreate

# Check logs for errors
docker-compose logs
```

#### Issue: SQL Server connection failed
**Solution:**
```bash
# Verify SQL Server is running
docker ps | grep sqlserver

# Check connection string in .env
# Ensure SQL_PASSWORD is correct

# Restart SQL Server container
docker-compose restart sqlserver

# Wait 10 seconds for SQL Server to be ready
sleep 10
docker-compose up -d
```

#### Issue: Frontend can't reach backend API
**Solution:**
```bash
# Check backend services are running
curl http://localhost:5005/health

# Verify REACT_APP_API_URL in frontend config
cat ventixereact/.env

# Check CORS settings in backend services
# Should allow frontend origin: http://localhost:5173

# Restart frontend
docker-compose restart ventixe-react
```

#### Issue: Qdrant vector database connection error
**Solution:**
```bash
# Check Qdrant is running
docker ps | grep qdrant

# Verify Qdrant dashboard
curl http://localhost:6333/health

# Check Qdrant port mapping
docker logs qdrant
```

#### Issue: Database migration fails
**Solution:**
```bash
# Check connection string
echo $CONNECTION_STRING

# Verify database exists
docker exec ventixe-sqlserver sqlcmd -S localhost -U sa -P $SQL_PASSWORD -Q "SELECT name FROM sys.databases"

# Manually create database if needed
docker exec ventixe-sqlserver sqlcmd -S localhost -U sa -P $SQL_PASSWORD -Q "CREATE DATABASE EventDb"

# Retry migration
cd event-service
dotnet ef database update
```

#### Issue: Port already in use
**Solution:**
```bash
# Find process using port (example: 5001)
# Windows
netstat -ano | findstr :5001

# Kill process
taskkill /PID <PID> /F

# Or change port in docker-compose.override.yml
```

#### Issue: Out of memory errors
**Solution:**
```bash
# Increase Docker memory allocation
# Docker Desktop > Preferences > Resources > Memory

# Or reduce service count
docker-compose up -d event-service booking-service frontend
```

### Debug Commands

```bash
# View all running services
docker-compose ps

# Check service health
docker-compose ps --filter "status=running"

# View service logs (last 50 lines)
docker-compose logs --tail=50 event-service

# Execute command in container
docker exec -it ventixe-event-service dotnet --version

# Test API endpoint
curl -v http://localhost:5005/api/events

# Check database connection
docker exec ventixe-sqlserver sqlcmd -S localhost -U sa -P $SQL_PASSWORD -Q "SELECT @@VERSION"

# Qdrant diagnostics
curl http://localhost:6333/health
```

### Getting Help

- **Check logs**: `docker-compose logs -f`
- **Review error messages**: Look for stack traces in console output
- **Check .env configuration**: Verify all required variables are set
- **Database connection**: Ensure SQL Server is accessible
- **Network issues**: Verify Docker network: `docker network ls`

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Team & Support

- **Project Lead**: [Project Owner Name]
- **Documentation**: See individual service READMEs for detailed information
- **Issues**: Report via [GitHub Issues](https://github.com/your-org/Ventixe-All/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/Ventixe-All/discussions)

---

## 🔗 Useful Links

- **Microsoft Semantic Kernel**: https://github.com/microsoft/semantic-kernel
- **Qdrant Vector Database**: https://qdrant.tech/
- **ASP.NET Core Docs**: https://docs.microsoft.com/aspnet/core
- **React Docs**: https://react.dev
- **Docker Docs**: https://docs.docker.com
- **Azure Web Apps**: https://azure.microsoft.com/services/app-service/web/

---

**Last Updated**: April 2026  
**Version**: 1.0.0
