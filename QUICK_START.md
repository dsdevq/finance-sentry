# 🚀 Running Finance Sentry Full Stack - Quick Start

## All Issues Fixed ✅

Your full-stack debug configuration is now ready. Here's what was fixed:

### Core Fixes Applied
1. ✅ Database migrations auto-run on API startup
2. ✅ CORS enabled for frontend (localhost:4200)
3. ✅ Health check endpoint for Docker
4. ✅ Frontend environment configuration
5. ✅ Backend development settings
6. ✅ Docker Compose environment variables
7. ✅ VSCode debug tasks

---

## How to Run the Application

### Option 1: VSCode Debug (Recommended)
1. **Update Plaid credentials** (optional - sandbox defaults work):
   ```bash
   # Edit docker/.env
   PLAID_CLIENT_ID=your_id_here
   PLAID_SECRET=your_secret_here
   ```

2. **Press F5** in VS Code
3. **Select**: "Full Stack: Backend + Frontend + DB"
4. Wait for services to start (~30 seconds)
5. Chrome opens at http://localhost:4200

### Option 2: Manual Docker + Local Run
```bash
# Start Docker services only
cd docker
docker-compose -f docker-compose.override.dev.yml up -d

# In separate terminal, run backend
cd backend/src/FinanceSentry.API
dotnet run

# In another terminal, run frontend
cd frontend
npm start
```

### Option 3: Just Docker (No debugging)
```bash
cd docker
docker-compose -f docker-compose.override.dev.yml up
```

---

## What Each Configuration Does

### Docker Compose Services
- **postgres**: PostgreSQL database (port 5432)
- **api**: .NET 9 backend (port 5000)

### Frontend
- Angular dev server (port 4200)
- Auto-compiles and hot-reloads

### Backend
- Auto-runs database migrations
- Swagger UI at http://localhost:5000/swagger
- API routes at http://localhost:5000/api

---

## Verifying Everything Works

1. **Docker is running**:
   ```bash
   docker ps
   # Should show postgres and api containers
   ```

2. **Backend is healthy**:
   ```bash
   curl http://localhost:5000/api/v1/health
   # Should return: {"status":"healthy","timestamp":"..."}
   ```

3. **Frontend loads**:
   - Open http://localhost:4200
   - Should see Angular app (Bank Sync module)

4. **Database connected**:
   - Check docker logs for migration output
   - ```bash
     docker logs finance-sentry-api | grep -i migration
     ```

---

## Debugging Tips

### Backend Debugging in VSCode
- Breakpoints work in C# code
- Watch variables, call stack visible
- Step through code with F10/F11

### Frontend Debugging in Chrome
- DevTools opens automatically
- Breakpoints work in TypeScript
- Source maps enabled

### Common Issues

**Port already in use?**
```bash
# Kill process using port 5000 (backend)
lsof -i :5000 | grep LISTEN | awk '{print $2}' | xargs kill -9

# Kill process using port 4200 (frontend)
lsof -i :4200 | grep LISTEN | awk '{print $2}' | xargs kill -9

# Kill process using port 5432 (database)
lsof -i :5432 | grep LISTEN | awk '{print $2}' | xargs kill -9
```

**Docker containers won't start?**
```bash
# Clean up old containers
docker-compose -f docker/docker-compose.override.dev.yml down -v

# Start fresh
docker-compose -f docker/docker-compose.override.dev.yml up -d
```

**Backend migrations fail?**
```bash
# Check logs
docker logs finance-sentry-api

# If database is corrupted, reset it
docker-compose -f docker/docker-compose.override.dev.yml down -v
docker-compose -f docker/docker-compose.override.dev.yml up -d
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Development Machine                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────┐         ┌──────────────────────┐  │
│  │   Chrome Browser     │         │   VS Code Debugger   │  │
│  │  http://localhost:4200        │  .NET & TypeScript   │  │
│  └──────────────────────┘         └──────────────────────┘  │
│           │                                 │                │
│           ▼                                 ▼                │
│  ┌──────────────────────┐         ┌──────────────────────┐  │
│  │  Angular Dev Server  │◄───────►│   .NET 9 Backend     │  │
│  │  npm start (4200)    │         │  dotnet run (5000)   │  │
│  └──────────────────────┘         └──────────────────────┘  │
│           │                                 │                │
│           └─────────────────┬────────────────┘                │
│                             ▼                │                │
│                      ┌──────────────────┐    │                │
│                      │  PostgreSQL 14   │◄───┘                │
│                      │ port 5432 (5432) │                     │
│                      └──────────────────┘                     │
│                                                              │
│             (All in Docker containers)                       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Next Steps

1. ✅ **Configuration complete** - Ready to debug
2. 🚀 **Start the application** - Press F5 or run docker-compose
3. 🧪 **Test the application** - Create bank accounts, sync transactions
4. 📝 **Feature 002 Implementation** - Ready to begin after testing

Enjoy debugging! 🎉
