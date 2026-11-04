# Helpdesk Ticketing API

A backend REST API for managing customer support operations, built with **C#, ASP.NET Core 8, and Microsoft SQL Server**.

The application provides a structured ticket management system where customers can submit support requests, agents can manage and respond to tickets, and support teams can track ticket progress through a defined workflow.

The project was developed incrementally, focusing on backend architecture, database design, business logic, and testing. **Claude (Anthropic) was used as an AI development assistant** throughout the development process to help explore implementation approaches, work through technical challenges, debug issues, and review code.

---

## Project Overview

Managing customer support requests can become difficult when tickets, assignments, and conversations are scattered across different systems.

This API addresses that problem by providing a centralized backend for:

- Creating and managing customer support tickets.
- Assigning tickets to support agents.
- Tracking ticket status and priority.
- Maintaining conversations through ticket comments.
- Enforcing business rules throughout the ticket lifecycle.
- Generating basic support statistics, including average resolution time.

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Backend development |
| ASP.NET Core 8 | REST API framework |
| Microsoft SQL Server | Relational database |
| ADO.NET | Database connectivity |
| Stored Procedures | Database operations and business-related queries |
| Swagger / OpenAPI | API documentation and testing |
| xUnit | Unit testing |
| Moq | Dependency mocking |

## Architecture

The application follows a layered architecture to separate HTTP handling, business logic, and database operations.

```text
                HTTP Requests
                      |
                      v
                 Controllers
                      |
                      v
                   Services
              (Business Logic)
                      |
                      v
                Repositories
              (Data Access)
                      |
                      v
                 SQL Server
```

**Layer responsibilities:**

- **Controllers:** Handle incoming HTTP requests, validate input, and return appropriate HTTP responses.
- **Services:** Contain application logic and enforce business rules.
- **Repositories:** Handle database communication, including stored procedure execution.
- **Database:** Stores application data and provides SQL views, stored procedures, constraints, and indexes.

This separation makes the application easier to maintain, test, and extend.

## Ticket Lifecycle

Tickets follow a predefined workflow to ensure that status changes remain consistent.

```text
Open → InProgress → Resolved → Closed
  ↑          |
  └──────────┘
```

**Workflow rules:**

- New tickets are created with an `Open` status.
- Open tickets can be assigned and moved to `InProgress`.
- Tickets can be marked as `Resolved` once the issue is addressed.
- Resolved tickets can be reopened and moved back to `InProgress`.
- Open tickets can also be closed directly.
- Closed tickets are final and cannot be reopened.

The workflow rules are enforced in the service layer rather than relying entirely on API clients.

## Features

### Ticket Management
- Create and retrieve support tickets.
- Filter tickets by status.
- Retrieve individual tickets together with their comments.
- Track ticket priority and category.

### Agent Management
- Assign tickets to support agents.
- Manage ticket ownership through database operations.

### Customer Management
- Create and retrieve customer records.
- Retrieve open tickets belonging to a specific customer.

### Comments and Communication
- Add comments to existing tickets.
- Maintain a conversation history for each ticket.

### Reporting and Statistics
- Retrieve ticket statistics.
- Calculate average ticket resolution time.
- Use SQL views to simplify reporting queries.

### Validation and Error Handling
- Validate incoming request data.
- Enforce ticket workflow restrictions.
- Return appropriate HTTP status codes and error responses.

## API Endpoints

### Tickets

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/tickets` | Retrieve all tickets, optionally filtered by status |
| GET | `/api/tickets/{id}` | Retrieve a ticket with its comments |
| POST | `/api/tickets` | Create a new ticket |
| PUT | `/api/tickets/{id}/assign` | Assign a ticket to an agent |
| PUT | `/api/tickets/{id}/status` | Update ticket status |
| POST | `/api/tickets/{id}/comments` | Add a comment to a ticket |
| GET | `/api/tickets/stats` | Retrieve ticket statistics |

### Customers

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/customers` | Retrieve all customers |
| POST | `/api/customers` | Create a new customer |
| GET | `/api/customers/{id}/tickets/open` | Retrieve a customer's open tickets |

### Example Request

Create a new support ticket:

**POST** `/api/tickets`

```json
{
  "customerId": 1,
  "categoryId": 1,
  "title": "Cannot log in",
  "description": "Password reset email never arrives.",
  "priority": "High"
}
```

## Database Design

The application uses Microsoft SQL Server with a relational database structure.

The database consists of the following main tables:

| Table | Description |
|---|---|
| Customers | Stores customer information |
| Agents | Stores support agent information |
| Categories | Defines ticket categories |
| Tickets | Stores support tickets and their current status |
| TicketComments | Stores comments associated with tickets |

**Database implementation includes:**

- Primary and foreign key relationships.
- `CHECK` constraints for ticket status and priority.
- Indexes on frequently filtered columns.
- Stored procedures for selected database operations.
- SQL views for reporting and ticket statistics.

Database creation and configuration scripts are available in `sql/schema.sql`.

## Getting Started

### Prerequisites

Make sure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server (LocalDB, Developer Edition, or Docker).
- SQL Server Management Studio (SSMS) or Azure Data Studio (optional).

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd <repository-folder>
```
### 1.5 Database Configuration

The application uses Microsoft SQL Server for data persistence.

Before running the API, configure your own SQL Server connection string in `src/HelpdeskApi/appsettings.json`.

Replace the placeholder connection string with your local SQL Server configuration.

**Example (Windows Authentication):**

```json
{
  "ConnectionStrings": {
    "HelpdeskDb": "Server=localhost\\SQLEXPRESS;Database=HelpdeskDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Example (SQL Server Authentication):**

```json
{
  "ConnectionStrings": {
    "HelpdeskDb": "Server=localhost,1433;Database=HelpdeskDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

Make sure your SQL Server instance is running and that the `HelpdeskDb` database has been created using `sql/schema.sql`.

**Note:** The connection strings above are examples. Configure them according to your local SQL Server installation. Never commit real database passwords or sensitive credentials to GitHub.

### 2. Create the Database

Execute the SQL script located at:

```text
sql/schema.sql
```

Run it using SSMS or Azure Data Studio. The script creates the database structure, constraints, indexes, stored procedures, views, and sample data.

Alternatively, you can run SQL Server using Docker:

```bash
docker run \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='YourStrong!Pass1' \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Use the following connection string:

```text
Server=localhost,1433;Database=HelpdeskDb;User Id=sa;Password=YourStrong!Pass1;TrustServerCertificate=True;
```

### 3. Configure the Connection String

Update the database connection string in:

```text
src/HelpdeskApi/appsettings.json
```

Make sure it matches your SQL Server configuration.

For real deployments, store credentials in environment variables or user secrets rather than committing passwords to source control.

### 4. Run the Application

From the repository root:

```bash
dotnet run --project src/HelpdeskApi
```

### 5. Explore the API

Once the application is running, open Swagger using the URL displayed in your terminal and append:

```text
/swagger
```

You can explore the available endpoints and send requests directly through the Swagger interface.

### 6. Run the Tests

Execute the test suite:

```bash
dotnet test tests/HelpdeskApi.Tests
```

## Testing

The project includes unit tests built with **xUnit** and **Moq**.

Testing focuses on application behavior and business logic, including ticket workflow rules and service-layer operations.

The goal is to verify that important application rules work as expected without requiring a live database for every unit test.

## Development Process & AI Assistance

This project was built step by step, with an emphasis on understanding the implementation rather than simply generating a working API.

The development process involved:

1. Defining the problem and identifying the main entities and application requirements.
2. Designing the SQL Server database schema and relationships.
3. Structuring the ASP.NET Core application using controllers, services, and repositories.
4. Implementing ticket and customer management functionality.
5. Integrating SQL Server operations, including stored procedures and views.
6. Implementing ticket lifecycle rules and request validation.
7. Writing unit tests and troubleshooting implementation issues.
8. Reviewing and refining the overall architecture.

**AI-assisted development:** Claude was used throughout the project as a development assistant for technical explanations, implementation guidance, debugging, exploring alternative solutions, and reviewing code. Development decisions and implementation details were progressively worked through to build a better understanding of the technologies and architecture involved.

## Potential Improvements

The following features could be introduced in future iterations:

- JWT authentication and authorization with separate customer and agent roles.
- Pagination and sorting for ticket listings.
- More advanced filtering and search functionality.
- Entity Framework Core implementation for comparison with the repository-based ADO.NET approach.
- Integration and database testing.
- Docker Compose for easier application and database setup.
- Cloud deployment using AWS and Amazon RDS for SQL Server.

---

**Project Focus:** Backend Development · REST APIs · C# · ASP.NET Core · SQL Server · Database Design · Software Architecture · Unit Testing