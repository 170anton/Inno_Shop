# Inno Shop

A simple microservices-based application built with ASP.NET Core.  
It consists of two separate services:

- **UserService** – user management (registration, login, email confirmation, password reset, profile).
- **ProductService** – product CRUD operations tied to authenticated users.

---

## Architecture

```
┌──────────────────────┐       ┌──────────────────────┐
│ UserService API:5001 │◀──────┤ PostgreSQL user-db   │
│                      │       │ Port 5433            │
└──────────────────────┘       └──────────────────────┘
┌──────────────────────┐       ┌──────────────────────┐
│ProductService        │       │PostgreSQL prod-db    │
│ API:5002	       │◀──────┤Port 5434 	      │
└──────────────────────┘       └──────────────────────┘
┌──────────────────────┐
│ pgAdmin UI :5050     │
└──────────────────────┘
┌──────────────────────┐
│ MailHog UI :8025     │
└──────────────────────┘

```

---


## Local Setup

1. **Clone repository**  
   ```bash
   git clone https://github.com/170anton/Inno_Shop.git
   cd inno_shop
   ```

2. **Environment variables**  

   ```
   # UserService
   ASPNETCORE_ENVIRONMENT=Development
   ConnectionStrings__Default=Host=user-db;Port=5432;Database=UserServiceDb;Username=user;Password=userpassword
   Jwt__Key=242g8rfr3es8tg9ag89asr9jas49a6t5h7as3h67a5grs6g
   Jwt__Issuer=domain.com
   Jwt__Audience=domain.com
   Jwt__ExpireMinutes=30
   AppSettings__ClientUrl=http://localhost:4200

   # ProductService
   ConnectionStrings__Default=Host=product-db;Port=5432;Database=ProductServiceDb;Username=product;Password=productpassword
   ProductService__BaseUrl=http://localhost:5002
   ```

3. **Start all services**  
   ```bash
   docker-compose up --build
   ```

4. **Access UIs**  
   - UserService Swagger: http://localhost:5001/swagger  
   - ProductService Swagger: http://localhost:5002/swagger  
   - pgAdmin: http://localhost:5050 (admin@admin.com / admin)  
   - MailHog: http://localhost:8025  

---

## Running Tests

- **Unit Tests**  
  ```bash
  cd UserService/UserService.Tests && dotnet test
  cd ProductService/ProductService.Tests && dotnet test
  ```

- **Integration Tests**  
  ```bash
  cd UserService/UserService.IntegrationTests && dotnet test
  cd ProductService/ProductService.IntegrationTests && dotnet test
  ```

 Integration tests use an in-memory database by setting `ASPNETCORE_ENVIRONMENT=IntegrationTests`.

---

## API Endpoints

### UserService

- `POST /api/auth/register` – user sign-up  
- `POST /api/auth/login` – user login
- `GET /api/auth/confirmemail` – email confirmation  
- `POST /api/auth/forgotpassword` – request password reset  
- `POST /api/auth/resetpassword` – reset password 

### ProductService

- `GET /api/products` – get all products of the current user  
- `GET /api/products/{id}` – get a single product  
- `POST /api/products` – create a product  
- `PUT /api/products/{id}` – update a product  
- `DELETE /api/products/{id}` – delete a product  
- `GET /api/products/search` – search by name and price range  
- `PUT /api/products/deactivate/{userId}` – deactivate all products of a user  
- `PUT /api/products/activate/{userId}` – activate all products of a user  

All secured endpoints require a valid JWT in the `Authorization: Bearer {token}` header.

---
