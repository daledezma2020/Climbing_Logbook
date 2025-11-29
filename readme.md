# Climbing Logbook

Rock climbing session logbook and social media web app.

## Tech Stack

**Frontend:** React 19, TypeScript, Vite
**Backend:** ASP.NET Core (.NET 9), C#

## Setup

### Backend (API)
```bash
cd api
dotnet restore
dotnet run
```
API runs on `https://localhost:7000` (or configured port)

### Frontend (Web)
```bash
cd web
npm install
npm run dev
```
Web app runs on `http://localhost:5173`

## Development

- API includes Swagger UI at `/swagger` in development mode
- CORS configured to allow frontend at `localhost:5173`
