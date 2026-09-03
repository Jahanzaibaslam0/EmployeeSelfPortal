# HRMS LocalDB setup

Creates **HRMSDB** on `(LocalDB)\MSSQLLocalDB` with data files under `D:\Project\DATA`, all app tables, and seed lookups.

## One-click

Double-click:

`D:\Project\HRMS\Database\SetupLocalDB.bat`

Requires: SQL Server LocalDB + `sqlcmd` + `sqllocaldb` on PATH.

## Manual (SSMS / Azure Data Studio)

Connect to `(LocalDB)\MSSQLLocalDB`, then run in order:

1. `LocalDB_CreateDatabase.sql`
2. `Script.sql`
3. `UserSecurity_Script.sql`
4. `LocalDB_MissingTables.sql`

## After setup

1. Start `D:\Project\HRMS\RUN.bat`
2. Login: **admin** / **Admin@123** (seeded by the app on startup)

`appsettings.json` already points at this LocalDB catalog.
