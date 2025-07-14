# OneIncTestApp

This repository contains the **OneIncTestApp**, a project designed to process jobs using SignalR, Angular, and .NET Core, hosted with Docker and Nginx for reverse proxy and basic authentication.

---

## **Project Structure**

- **OneIncTestApp.API**: The backend API built with .NET Core, responsible for job processing and SignalR communication.
- **OneIncTestApp.Tests**: Unit tests for the backend API services.
- **OneIncTestApp.Web**: The frontend Angular application for interacting with the API.
- **nginx**: Configuration files for Nginx, used as a reverse proxy and for serving the Angular application.
- **docker-compose.yml**: Docker Compose file for setting up multi-container deployment (API, Web, and Nginx).
- **OneIncTestApp.sln**: The solution file for the .NET project.

---

## **Setup Instructions**

### **Prerequisites**

Before you begin, ensure you have the following installed:
1. **Docker**: [Install Docker](https://www.docker.com/)
2. **Docker Compose**: [Install Docker Compose](https://docs.docker.com/compose/install/)
3. **Node.js**: [Install Node.js](https://nodejs.org/) (Angular CLI requires a minimum Node.js version of v20.19 or v22.12) (for Angular development)
4. **.NET SDK**: [Install .NET SDK](https://dotnet.microsoft.com/) (for API development)

---

### **Running Locally**

#### **1. Clone the Repository**
```bash
git clone <repository-url>
cd OneIncTestApp
```

#### **2. Build and Run Docker containers**
```bash
cd OneIncTestApp
docker compose build
docker-compose up --build
```

#### **3. Open and use app**
``` 
Open application wirh url - http://localhost:8080/
Credendials:
Username: ineincadmusr
Password: w66cbZxkwDjwjgHBcZd3
 ```

In Angular app, for local development, in processing.service.ts change:
private apiUrl = 'https://localhost:<your_port>/processing';
private hubUrl = 'https://localhost:<your_port>/processingHub';
