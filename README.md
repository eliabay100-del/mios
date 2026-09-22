# MIOS Inventory Management System

MIOS is a VB.NET Windows Forms desktop application for managing spare-part inventory and the business operations connected to it. It uses SQL Server Express as its local database and brings inventory, purchasing, sales/issues, customers, suppliers, payments, returns, expenses, accounting, security, reporting, and audit history into one system.

## What MIOS Does

MIOS is designed to manage the complete operational lifecycle of spare parts:

- Create and manage spare-part/product records
- Organize products into categories
- Manage suppliers and customers
- Receive purchased stock
- Track individual purchase batches
- Deduct stock using FIFO (First In, First Out) logic
- Issue/sell parts to customers
- Monitor current inventory and low-stock items
- Record stock movements through a stock-card history
- Process customer returns and supplier returns
- Adjust inventory after physical stock counts
- Record payments against supported sales and purchase documents
- Record business expenses
- Post balanced accounting journal entries
- Produce financial and operational reports
- Export selected records to CSV
- Manage users, roles, and permissions
- Maintain an audit trail for important operations
- Back up the SQL Server database
- Switch the interface between English and Amharic

The goal is to keep operational transactions connected to the inventory and financial records they affect instead of maintaining separate disconnected records.

## Main Modules

### Dashboard
The dashboard provides a central overview of the business and quick access to the major functions.

It displays:

- Stock on hand
- Number of purchase batches
- Low-inventory count
- Sales total for the current day
- Quick actions for inventory, purchasing, sales, suppliers, customers, and reporting
- Administrative actions based on the current user's permissions

### Product and Inventory Management
The product module maintains spare-part records and their current stock quantities.

Users can:

- Add new spare parts
- Add stock to existing parts
- Assign categories
- Maintain quantities
- Set reorder levels
- Search and filter inventory
- Identify low-stock items
- Review current inventory balances

Inventory adjustments record the previous quantity, new quantity, difference, reason, and responsible user.

### Purchasing and Purchase Batches
MIOS tracks received stock through purchase batches.

Purchasing records can retain:

- Supplier
- Purchase receipt
- Part number
- Quantity received
- Quantity remaining
- Purchase cost
- Purchase history

This batch-level tracking is important for FIFO stock deduction and for understanding where current inventory came from.

### FIFO Inventory Control
When parts are issued, MIOS uses the oldest available purchase batches first.

The FIFO workflow helps maintain:

- Accurate remaining quantities by purchase batch
- Traceable stock consumption
- Consistent stock-cost handling
- Protection against issuing more inventory than is available

The Purchase Batches screen provides a dedicated view of these records.

### Sales and Issue Processing
The Issue Spare Part workflow records inventory leaving the business.

The process checks stock availability, deducts available quantities using FIFO logic, records the issue/sale, and creates the corresponding stock movement and financial records where applicable.

Issued/sold transactions can be reviewed through the Sold Items area.

### Customers
The customer module stores customer information and connects customers to sales and return activity.

It supports customer registration, lookup, sales relationships, and customer-return processing.

### Suppliers
The supplier module stores supplier information used throughout purchasing.

It supports supplier records, purchase relationships, purchase receipts, and supplier returns.

### Stock Card and Movement History
The Stock Card provides an audit-oriented history of inventory movement.

Movements can include:

- Purchases / stock-in
- Sales or issues / stock-out
- Customer returns
- Supplier returns
- Inventory adjustments

Each movement can include a reference and remarks, allowing users to investigate how the current inventory balance was produced.

### Payments
MIOS includes a payment workflow for supported sales and purchase documents.

Current payment methods include:

- Cash
- Bank transfer
- Card
- Other

Payment records contain the related document, amount, payment method, and notes. Payment processing is connected to the accounting lifecycle.

### Returns
MIOS supports both customer and supplier returns.

Customer returns:

- Reference a sales invoice
- Record the returned part and quantity
- Record amount and reason
- Restore the returned quantity to inventory
- Create a stock movement
- Create the related accounting entry

Supplier returns:

- Reference a purchase receipt
- Record the returned part and quantity
- Reduce the remaining purchase-batch quantity
- Reduce inventory
- Create a stock movement
- Create the related accounting entry

### Inventory Adjustments
Physical counts can differ from the system quantity. The adjustment module allows authorized users to correct inventory while preserving an explanation.

Each adjustment records:

- Part number
- Previous quantity
- New quantity
- Difference
- Reason
- User responsible

The adjustment is also reflected in the Stock Card and audit history.

### Expenses
The Expense module records business expenses as part of the financial lifecycle and makes them available to the reporting/accounting functionality.

### Accounting
MIOS contains a centralized AccountingService for financial posting.

The accounting layer includes:

- Chart of accounts
- Journal entries
- Journal lines
- Debit and credit validation
- Transaction references
- Accounting audit records

Journal entries are checked so total debits equal total credits before they are posted.

Supported operational workflows can create accounting records for sales, purchases, payments, returns, expenses, and related transactions.

### Financial Reports
The Financial Reports module provides access to financial information generated from the accounting records, allowing management to review business activity without manually reconstructing transactions.

### User Management and Security
MIOS uses role-based access control.

The application includes roles such as:

- Administrator
- Manager
- Storekeeper
- Accountant

Permissions can restrict sensitive operations including:

- User management
- Payment management
- Inventory adjustments
- Returns
- Expenses
- Financial reports

Important operations can also be written to an audit log.

### Password Security
Passwords are not stored as plain text.

New passwords use PBKDF2-SHA256 with per-password salts. Legacy SHA-256 passwords can be upgraded automatically after a successful login.

### Database Backup
Administrators have access to a database backup function from the dashboard.

Backups are important before destructive operations such as resetting application data.

### Data Export
MIOS supports CSV export for selected datasets, including:

- Customers
- Suppliers
- Purchase batches
- Issued/sold items
- Stock-card records

### Categories
Categories can be managed inside the application and are used to organize spare parts and filter inventory.

### Language Support
MIOS includes English and Amharic language support. The language service can apply translations across forms and controls.

## Typical Business Workflow

A normal inventory lifecycle can look like this:

1. Create categories and supplier records.
2. Add a new spare part or receive stock for an existing part.
3. MIOS creates/updates the product and records the purchase batch.
4. The stock-in movement appears in the Stock Card.
5. Inventory List shows the updated quantity.
6. A part is sold or issued through Issue Spare Part.
7. MIOS checks available stock and consumes the oldest available purchase batches first.
8. The sale/issue and stock-out movement are recorded.
9. A payment can be recorded against the related document.
10. Returns can be processed when goods come back from a customer or are sent back to a supplier.
11. Physical-count differences can be corrected through Inventory Adjustment with a reason.
12. Expenses and other financial activity can be recorded.
13. Financial and operational reports can be reviewed.
14. Administrators can back up the database.

## Dashboard Functions

| Function | Purpose |
|---|---|
| Add Spare Part | Create products and receive inventory |
| Issue Spare Part | Sell/issue inventory and reduce stock |
| Inventory List | Search and review current inventory |
| Purchase Batches | Review purchase batches and FIFO quantities |
| Sold Items | Review issued/sold transactions |
| Stock Card | Review inventory movements |
| Suppliers | Manage suppliers |
| Customers | Manage customers |
| Record Payment | Record supported payments |
| Users & Roles | Manage users and permissions |
| Manage Categories | Maintain categories |
| Adjust Stock | Correct inventory with an audit reason |
| Financial Reports | Review financial information |
| Record Expense | Record business expenses |
| Process Return | Process customer and supplier returns |
| Database Backup | Back up the database |

Administrative functions are shown or permitted according to the logged-in user's role and permissions.

## Technology Stack

- Visual Basic .NET
- Windows Forms
- .NET Framework 4.7.2
- Microsoft SQL Server Express
- ADO.NET / System.Data.SqlClient
- Visual Studio

Database configuration:

- Server: .\SQLEXPRESS
- Database: Inventory
- Schema: mn
- Authentication: Windows Integrated Security

Connection string:

    Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;

The Windows service SQL Server (SQLEXPRESS) must be running.

## Requirements

Install:

- Visual Studio with the .NET desktop development workload
- .NET Framework 4.7.2 developer tools
- SQL Server Express with the SQLEXPRESS instance
- SQL Server command-line tools (sqlcmd)

## Database Setup

Open PowerShell in the project directory and run the database scripts in this order.

### 1. Create the base database

    sqlcmd -S ".\SQLEXPRESS" -E -C -i ".\create_inventory_database.sql"

### 2. Add purchasing, sales, payment, and accounting support

    sqlcmd -S ".\SQLEXPRESS" -E -C -d Inventory -i ".\upgrade_accounting_lifecycle.sql"

### 3. Add security roles and audit logging

    sqlcmd -S ".\SQLEXPRESS" -E -C -d Inventory -i ".\upgrade_security_roles.sql"

### 4. Add adjustments, returns, expenses, and reporting support

    sqlcmd -S ".\SQLEXPRESS" -E -C -d Inventory -i ".\upgrade_operations.sql"

The scripts create and upgrade the Inventory database and mn schema used by MIOS.

## Running the Application

1. Open Mios_inventory_management.sln in Visual Studio.
2. Select Build > Rebuild Solution.
3. Press F5 or click Start.
4. On an empty database, MIOS opens first-run user setup.
5. Create the initial username and password.
6. Sign in.
7. Configure categories, suppliers, and spare parts.
8. Begin receiving, issuing, tracking, and reporting inventory.

## Resetting Application Data

The repository includes reset_inventory_database.sql.

This script is destructive. It removes application data such as:

- Users and passwords
- Customers
- Suppliers
- Spare parts
- Purchase batches
- Purchase receipts
- Sales invoices
- Payments
- Journal entries
- Issued items
- Stock-card history
- Other related operational records

Required lookup information is retained where the application depends on it.

Run the reset script only when a clean application database is intentionally required:

    sqlcmd -S ".\SQLEXPRESS" -E -C -d Inventory -i ".\reset_inventory_database.sql"

Back up the database before using the reset script.

## Project Structure

| Component | Purpose |
|---|---|
| Form1.vb | Main MIOS dashboard |
| LoginNew.vb | Active login screen |
| FirstRunSetupForm.vb | Initial user setup |
| Add_SparePart.vb | Product and stock receiving |
| Issue_SparePart.vb | Sales/issue workflow |
| View_Inventory.vb | Inventory view |
| View_PurchaseBatch.vb | Purchase-batch/FIFO view |
| View_SoldItem.vb | Sold/issued item view |
| View_StockCard.vb | Stock movement audit view |
| Supplier.vb | Supplier management |
| Customer.vb | Customer management |
| PaymentForm.vb | Payment recording |
| ReturnForm.vb | Customer/supplier returns |
| InventoryAdjustmentForm.vb | Inventory adjustments |
| ExpenseForm.vb | Expense recording |
| FinancialReportsForm.vb | Financial reports |
| UserManagementForm.vb | User and role management |
| CategoryManagementForm.vb | Category management |
| BackupForm.vb | Database backup |
| AccountingService.vb | Journal posting and accounting audit logic |
| SecurityContext.vb | Current-user and permission context |
| DBModule.vb | Database connection support |
| Theme.vb | Shared UI styling |
| LanguageService.vb | English/Amharic language support |

## Architecture

MIOS is designed around connected business transactions.

Examples:

Purchase -> Purchase Receipt -> Purchase Batch -> Stock-In -> Inventory Balance

Sale/Issue -> Sales Invoice -> Stock-Out -> Payment -> Accounting

Customer Return -> Return Record -> Stock-In -> Accounting

Supplier Return -> Return Record -> Purchase Batch Reduction -> Stock-Out -> Accounting

Inventory Adjustment -> Quantity Change -> Stock Card -> Audit Log

This structure helps keep operational activity, inventory movement, financial records, and audit information connected.

## Current Scope

MIOS is primarily a spare-part inventory and business operations management system.

It is a local Windows desktop application backed by SQL Server Express rather than a cloud/SaaS system.

The current scope focuses on centralizing inventory, purchasing, sales/issues, customers, suppliers, payments, returns, expenses, accounting records, security, reporting, and auditability in one application.

## Project Notes

- Active startup form: LoginNew
- Main dashboard: Form1.vb
- Active user table: mn.UUser
- Main database schema: mn
- Target framework: .NET Framework 4.7.2
- New passwords use PBKDF2-SHA256 with per-password salts
- Legacy SHA-256 passwords are upgraded after successful login
- Sensitive operations use role and permission checks
- Important accounting and operational actions can be recorded in the audit log
- The project is separate from the pmas application
