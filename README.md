# MIOS Inventory Management

MIOS is a VB.NET Windows Forms application for managing spare-part inventory.
It tracks suppliers, customers, incoming purchase batches, issued parts, stock
movements, and inventory balances using SQL Server.

## Features

- First-run user creation and password login
- Add new spare parts or receive stock for existing parts
- Supplier and customer lookup
- FIFO stock deduction when parts are issued
- Inventory search and category filtering
- Purchase-batch tracking
- Issued/sold-item reporting
- Stock-card audit trail for stock-in and stock-out movements
- CSV export for customers, suppliers, purchase batches, issued items, and stock cards
- Dashboard statistics based on current database data

## Technology

- Visual Basic .NET Windows Forms
- .NET Framework 4.7.2
- SQL Server Express
- SQL Server schema `mn`

## Requirements

Install the following before opening the solution:

- Visual Studio with the .NET desktop development workload
- .NET Framework 4.7.2 developer tools
- SQL Server Express with the `SQLEXPRESS` instance
- SQL Server command-line tools (`sqlcmd`)

The application is configured to use this local connection:

```text
Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;
```

Make sure the Windows service **SQL Server (SQLEXPRESS)** is running.

## Database setup

Open PowerShell in this project directory:

```powershell
cd "C:\Users\eliab\Desktop\mios vs\mios"
```

Create the database and tables:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -i ".\create_inventory_database.sql"
```

The setup script creates the `Inventory` database, the `mn` schema, all tables,
and the default `General` category and `New` condition required by the add-part
form.

## Reset all application data

`reset_inventory_database.sql` permanently deletes all MIOS users, passwords,
customers, suppliers, parts, purchase batches, issued items, and stock-card
history. It keeps the category and condition lookup rows.

Run it only when a clean database is intended:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -d Inventory -i ".\reset_inventory_database.sql"
```

## Run the application

1. Open `Mios_inventory_management.sln` in Visual Studio.
2. Select **Build > Rebuild Solution**.
3. Press **F5** or click the green **Start** button.
4. On an empty database, MIOS opens the first-run user setup screen.
5. Create a username and a password with at least six characters.
6. Sign in and begin adding suppliers and spare parts.

## Typical workflow

1. Add stock through **Add Spare Part**. This creates or updates the part,
	 creates a purchase batch, and records an `IN` stock-card movement.
2. Issue parts through **Issue Spare Part**. MIOS checks available stock and
	 deducts the oldest available purchase batches first.
3. Review current quantities in **Inventory List**.
4. Review received batches in **Purchase Batches**.
5. Review issues and customers in **Sold Items** and **Customers**.
6. Use **Stock Card** to inspect inventory movements and balances.

## Project notes

- The active startup form is `LoginNew`.
- The active user table is `mn.UUser`.
- Passwords are stored as SHA-256 hashes by the current application.
- The project does not contain a Visual Studio solution migration or a database
	backup. Back up the SQL Server database before using reset scripts.
- The project is separate from the `pmas` application.