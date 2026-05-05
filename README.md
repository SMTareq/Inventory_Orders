# Inventory & Order Management System

A full-featured Inventory & Order Management web app built with **ASP.NET Core 8 MVC**, **MS SQL Server**, **EF Core + Dapper**, and **jQuery AJAX**.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| ORM (Write) | Entity Framework Core 8 |
| ORM (Read) | Dapper |
| Database | MS SQL Server |
| Auth | ASP.NET Core Identity |
| Frontend | Razor Views, Bootstrap 5.3, jQuery 3.7 |
| Logging | Serilog (Console + File) |

---

## Project Structure

```
InventoryOrderSystem/
├── Controllers/
│   ├── AccountController.cs    # Register / Login / Logout
│   ├── HomeController.cs       # Dashboard
│   ├── ProductController.cs    # Full CRUD + AJAX search
│   └── OrderController.cs      # Orders + CSV export
│
├── Models/
│   ├── ApplicationUser.cs      # Extended Identity user
│   ├── Product.cs
│   └── Order.cs                # Order + OrderItem
│
├── ViewModels/
│   └── ViewModels.cs           # All DTOs / ViewModels
│
├── Repositories/
│   ├── IRepositories.cs        # Interfaces
│   ├── ProductRepository.cs    # Dapper reads / EF writes
│   └── OrderRepository.cs      # Dapper reads / EF writes + transactions
│
├── Services/
│   └── Services.cs             # Business logic layer
│
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext
│
├── Views/
│   ├── Account/                # Login, Register
│   ├── Home/                   # Dashboard
│   ├── Product/                # Index, Create, Edit
│   ├── Order/                  # Index, Create, Details
│   └── Shared/                 # _Layout, _AuthLayout
│
├── wwwroot/
│   ├── js/
│   │   ├── order.js            # Dynamic order form (jQuery AJAX)
│   │   └── site.js             # Global JS
│   └── css/
│       └── site.css            # Custom styles
│
├── Database/
│   └── setup.sql               # Manual DB setup (alternative to EF migrations)
│
└── Migrations/                 # EF Core migration files
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, Express, or full)
- Visual Studio 2022+ or VS Code with C# extension

### 1. Clone & Configure

```bash
git clone <repo-url>
cd InventoryOrderSystem
```

Edit `appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=InventoryOrderDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 2. Apply Migrations (Recommended)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Or** run `Database/setup.sql` manually in SSMS to create tables and seed data.

### 3. Run

```bash
dotnet restore
dotnet run
```

Navigate to `https://localhost:5001` and register your first account.

---

## Features

### Authentication
- Register / Login / Logout via ASP.NET Core Identity
- All routes protected — redirects to `/Account/Login` if unauthenticated
- Logged-in user shown in navbar dropdown

### Product Management
- **List** with server-side pagination (10/page)
- **AJAX live search** — debounced, searches by Name or SKU without page reload
- **Create / Edit / Delete** with validation
- SKU uniqueness enforced at service layer
- Stock badges: 🟢 > 10, 🟡 ≤ 10, 🔴 = 0

### Order Management
- **Dynamic order form** — add/remove product rows with jQuery
- Auto-fills unit price when product is selected (AJAX fetch)
- Real-time line total and order total calculation
- Client-side stock validation before submit
- Duplicate product detection
- Server-side transaction: stock deduction + order save atomically

### Inventory Control
- Stock is deducted when order is placed (within a DB transaction)
- Stock is restored when order is deleted
- Out-of-stock products excluded from order dropdown
- Prevents over-ordering (client + server validation)

### Dashboard
- Total Sales, Total Orders, Total Products KPI cards
- Low Stock Alert (≤ 10 units)
- 5 most recent orders

---

## Architecture Notes

### Repository Pattern + Service Layer

```
Controller → Service → Repository → DB
```

- **Repositories** handle raw DB access (Dapper for reads, EF for writes)
- **Services** contain business logic (SKU uniqueness, stock validation, transaction orchestration)
- **Controllers** are thin — delegate everything to services

### EF Core for Writes, Dapper for Reads

| Operation | Tool | Reason |
|---|---|---|
| INSERT / UPDATE / DELETE | EF Core | Change tracking, transactions, migrations |
| SELECT (lists, reports) | Dapper | Performance, raw SQL control |

---

## Endpoints

| Method | URL | Description |
|---|---|---|
| GET | `/` | Dashboard |
| GET | `/Account/Login` | Login page |
| POST | `/Account/Login` | Authenticate |
| GET | `/Account/Register` | Register page |
| POST | `/Account/Register` | Create account |
| POST | `/Account/Logout` | Sign out |
| GET | `/Product` | Product list (paginated) |
| GET | `/Product/Search?term=` | AJAX search |
| GET | `/Product/GetProducts` | JSON list for order form |
| GET | `/Product/Create` | Create form |
| POST | `/Product/Create` | Save product |
| GET | `/Product/Edit/{id}` | Edit form |
| POST | `/Product/Edit` | Update product |
| POST | `/Product/Delete/{id}` | Delete product |
| GET | `/Order` | Order list |
| GET | `/Order/Create` | Order form |
| POST | `/Order/Create` | Place order |
| GET | `/Order/Details/{id}` | Order details |
| POST | `/Order/Delete/{id}` | Delete order (restores stock) |
| GET | `/Order/ExportCsv` | Download orders CSV |
