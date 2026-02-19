# InventoryAndOrders API 📦🧾

A FastEndpoints-based ASP.NET API for product inventory and order lifecycle management. This API is intended for an e-commerce business (something like Gumtree).

It includes:
- guest order creation and tracking
- stock reservation and unreservation on cancellation
- staff-protected management routes
- JWT authentication
- automated integration and service tests

## Key Features ✨

- **JWT auth + role-based protection** for staff operations.
- **Public product reads** that do not expose internal fields (`IsDeleted`, `TotalStock`, `ReservedStock`).
- **Order safety with transactions** in create/cancel workflows.
- **Guest order access** through `OrderNumber + X-Guest-Token`.
- **Validation with clear error messages** using FastEndpoints validators.
- **SQLite + Dapper** for a lightweight, testable persistence layer.

> [!IMPORTANT]
> - The register endpoint will make anyone staff! In a production setting, a unique staff token would be necessary to use this endpoint.

## Tech Stack 🛠️

- .NET `10.0`
- FastEndpoints
- SQLite + Dapper
- JWT Bearer authentication
- xUnit integration tests

## Packages 📚

### API Project (`InventoryAndOrders`) 🚀

| Package | Purpose | Link |
|---|---|---|
| `FastEndpoints` | Endpoint framework | https://www.nuget.org/packages/FastEndpoints |
| `FastEndpoints.Swagger` | OpenAPI/Swagger integration | https://www.nuget.org/packages/FastEndpoints.Swagger |
| `Dapper` | SQL mapping | https://www.nuget.org/packages/Dapper |
| `Microsoft.Data.Sqlite` | SQLite provider | https://www.nuget.org/packages/Microsoft.Data.Sqlite |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth middleware | https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer |
| `BCrypt.Net-Next` | Password hashing/verification | https://www.nuget.org/packages/BCrypt.Net-Next |
| `zxcvbn-core` | Password strength scoring | https://www.nuget.org/packages/zxcvbn-core |
| `Microsoft.AspNetCore.OpenApi` | OpenAPI support | https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi |

### Test Project (`InventoryAndOrders.Tests`) 🧪

| Package | Purpose | Link |
|---|---|---|
| `xunit` | Test framework | https://www.nuget.org/packages/xunit |
| `xunit.runner.visualstudio` | VS test runner integration | https://www.nuget.org/packages/xunit.runner.visualstudio |
| `Microsoft.NET.Test.Sdk` | Test host tooling | https://www.nuget.org/packages/Microsoft.NET.Test.Sdk |
| `Microsoft.AspNetCore.Mvc.Testing` | In-memory API integration testing | https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing |
| `coverlet.collector` | Coverage collection | https://www.nuget.org/packages/coverlet.collector |

## Folder Structure 🗂️

```text
InventoryAndOrders/
|- InventoryAndOrders/
|  |- Data/
|  |- DTOs/
|  |- Endpoints/
|  |  |- Auth/
|  |  |- Orders/
|  |  |- Products/
|  |- Enums/
|  |- Models/
|  |- Services/
|  |  |- Exceptions/
|  |- Swagger/
|  |- Validators/
|  |  |- Auth/
|  |  |- Order/
|  |  |- Product/
|  |- Program.cs
|  |- appsettings.json
|- InventoryAndOrders.Tests/
|  |- *EndpointTests.cs
|  |- *ServicesTests.cs
|  |- DatabaseIntegrityTests.cs
|  |- TestApiFactory.cs
|  |- ApiTestData.cs
|- InventoryAndOrders.sln
```

## Endpoint Overview 🌐

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/auth/register` | Public | Register account |
| `POST` | `/auth/login` | Public | Login and get JWT |
| `POST` | `/products` | Staff JWT | Create product |
| `GET` | `/products` | Public | List products (public response shape) |
| `GET` | `/products/{id}` | Public | Get product by id (public response shape) |
| `PATCH` | `/products/{id}` | Staff JWT | Update product |
| `DELETE` | `/products/{id}` | Staff JWT | Soft-delete product |
| `POST` | `/orders/create` | Public | Create order + reserve stock |
| `GET` | `/orders/{orderNumber}` | Public + `X-Guest-Token` | Get guest order |
| `POST` | `/orders/{orderNumber}/cancel` | Public + `X-Guest-Token` | Cancel order + unreserve stock |
| `GET` | `/staff/orders` | Staff JWT | List all orders (staff view) |
| `GET` | `/staff/orders/{orderNumber}` | Staff JWT | Get single order (staff view) |

## Setup ⚙️

1. Clone the repository:
```bash
git clone <your-repo-url>
cd InventoryAndOrders
```
2. Restore dependencies from the solution/project files:
```bash
dotnet restore
```
3. Run the API (database is created automatically on startup):
```bash
dotnet run --project InventoryAndOrders/InventoryAndOrders.csproj
```
4. Open Swagger UI:
`http://localhost:5167/swagger`

> [!NOTE]
> - JWT tokens expire after `2` hours.
> - After login, copy the returned token and use Swagger `Authorize` with `Bearer <token>`.
> - Staff routes require `Authorization: Bearer <token>`.
> - SQLite Database will automatically create when running API.

> [!IMPORTANT]
> - You do not need to install packages one-by-one; `dotnet restore` installs all dependencies from the `.csproj` files.
> - JWT settings and DB connection are already configured in `InventoryAndOrders/appsettings.json`.

> [!CAUTION]
> - Localhost URLs only work when the API is running on your local machine.

## Running Tests 🧪

Run all tests:

```bash
dotnet test InventoryAndOrders.Tests/InventoryAndOrders.Tests.csproj
```

Run one test class:

```bash
dotnet test InventoryAndOrders.Tests/InventoryAndOrders.Tests.csproj --filter ProductEndpointsTests
```

## Quick Test Values 🧷

Use these dummy values while testing:

- Staff account:
  - Username: `staffdemo`
  - Email: `staffdemo@example.com`
  - Password: `ValidPass123!`
- Product seed example:
  - Name: `Desk Lamp`
  - Price: `19.99`
  - TotalStock: `25`
- Order customer example:
  - FirstName: `Alex`
  - LastName: `Tester`
  - Email: `alex@example.com`
  - Phone: `1234567890`
  - Address: `123 Test Street, Testville, 12345, US`

## 📘 Endpoint Request Examples

Register
```json
POST /auth/register
Content-Type: application/json
{
  "username": "staffdemo",
  "email": "staffdemo@example.com",
  "password": "ValidPass123!"
}
```
> [!NOTE]
> - This endpoint will make any registered user staff!
> - In a production setting, I would have set up this endpoint to require a unique staff token (only accessible by staff members)

Login
```json
POST /auth/login
Content-Type: application/json
{
  "username": "staffdemo",
  "password": "ValidPass123!"
}
```

Create Product (Staff)
```json
POST /products
Authorization: Bearer {jwt-token}
Content-Type: application/json
{
  "name": "Desk Lamp",
  "price": 19.99,
  "totalStock": 25
}
```

List Products (Public)
```json
GET /products
```

Get Product by Id (Public)
```json
GET /products/1
```

Update Product (Staff)
```json
PATCH /products/1
Authorization: Bearer {jwt-token}
Content-Type: application/json
{
  "name": "Desk Lamp Pro",
  "price": 24.99
}
```

Delete Product (Staff)
```json
DELETE /products/1
Authorization: Bearer {jwt-token}
```

Create Order (Public)
```json
POST /orders/create
Content-Type: application/json
{
  "customerInfo": {
    "firstName": "Alex",
    "lastName": "Tester",
    "email": "alex@example.com",
    "phone": "1234567890"
  },
  "address": {
    "street": "123 Test Street",
    "city": "Testville",
    "postcode": "12345",
    "country": "US"
  },
  "items": [
    {
      "productId": 1,
      "quantity": 2
    }
  ]
}
```

Get Order (Guest)
```json
GET /orders/{orderNumber}
X-Guest-Token: {guest-token}
```

Cancel Order (Guest)
```json
POST /orders/{orderNumber}/cancel
X-Guest-Token: {guest-token}
```

Staff List Orders
```json
GET /staff/orders
Authorization: Bearer {jwt-token}
```

Staff Get Order
```json
GET /staff/orders/{orderNumber}
Authorization: Bearer {jwt-token}
```

## Important Behavior Notes 📝

> [!NOTE]
> - `username` and `email` are normalized to lowercase on registration/login checks.
> - Register rejects weak passwords (validator + zxcvbn score gate).
> - `CancelledAt` is `null` for active orders and populated when cancelled.

> [!IMPORTANT]
> - Guest order lookup/cancel requires `X-Guest-Token`.
> - Swagger includes the required cancel header via a custom operation processor.

>  [!CAUTION]
> - Create/cancel order flows run inside DB transactions and enforce stock rules.
> - Cancelling is allowed only when order state is `Pending` and payment is `Unpaid`.

---
