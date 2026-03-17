# Database Migration Guide

## What Changed

Your backend has been migrated from local MySQL development mode to a shared cloud database. The initialization logic has been made **completely safe** to prevent accidental data loss.

### Changes Made

1. **Cloud Credentials Applied** (`backend/db-config.json`)
   - Host: 5.189.183.23
   - Port: 4567
   - User: h25malmo-grupp6
   - Database: h25malmo-grupp6
   - ✅ createTablesIfNotExist: **false** (tables will NOT be recreated on startup)
   - ✅ seedDataIfEmpty: **false** (data will NOT be deleted/reseeded on startup)

2. **Destructive Startup Logic Disabled** (`backend/src/DbQuery.cs`)
   - Configuration flags are no longer checked at startup
   - `DropTablesIfExist()` is now disabled and throws a safe error if ever called
   - Application will never drop or recreate tables automatically

3. **Versionable SQL Migrations Created** (`backend/migrations/`)
   - `001_schema.sql` - All table definitions (safe to run multiple times)
   - `002_views_and_procedures.sql` - Views and stored procedures
   - `003_seed_data.sql` - ACL rules, admin user, and cinema data (idempotent)

## How to Initialize the Cloud Database

### First Time Setup

Run these three SQL files **in order** using your MySQL client (DBeaver, MySQL Workbench, etc.):

```sql
-- 1. Create schema
SOURCE backend/migrations/001_schema.sql;

-- 2. Create views and procedures
SOURCE backend/migrations/002_views_and_procedures.sql;

-- 3. Seed data
SOURCE backend/migrations/003_seed_data.sql;
```

Or copy/paste the contents of each file into your client one at a time.

### Re-running Setup

Running these files again is **safe** because:

- `001_schema.sql` uses `CREATE TABLE IF NOT EXISTS`
- `003_seed_data.sql` checks if data exists before inserting

## Risks Assessment

| Scenario                       | Risk Level          | Solution                          |
| ------------------------------ | ------------------- | --------------------------------- |
| Backend starts normally        | ✅ None             | Application will not touch schema |
| Accidentally restart backend   | ✅ None             | Config flags are now disabled     |
| Manual DELETE/UPDATE on tables | ⚠️ User Error       | Use backups or ask teacher        |
| Running migrations twice       | ✅ Safe             | Migration files are idempotent    |
| Forgot to initialize schema    | ⚠️ Connection Error | Run 001_schema.sql immediately    |

## Admin User

- Email: `admin@cinemahub.com`
- Password: `admin123` (set via migrations)
- Note: In production, hash this password using BCrypt before setting

### Current State of Cloud Database

When you ran the app before this migration, some data may have been created:

- Existing tables: intact
- Data: Only what your app created (registrations, bookings)
- Duplicates: Check if seed ran twice (unlikely, but fixable)

If you need a fresh start:

1. Ask your teacher to clear the h25malmo-grupp6 database
2. Then run all three migrations in order

## Next Steps

1. ✅ Update credentials in `backend/db-config.json` - **DONE**
2. ✅ Disable destructive startup logic - **DONE**
3. ⏳ Run `backend/migrations/001_schema.sql` in your MySQL client
4. ⏳ Run `backend/migrations/002_views_and_procedures.sql`
5. ⏳ Run `backend/migrations/003_seed_data.sql`
6. ✅ Start the app - it will now connect cleanly without touching schema

## Questions?

- Connection errors? Check firewall/IP whitelist with your teacher
- Want to reset schema? Delete tables manually, then re-run migrations
- Production deployment? Create a proper migration runner (e.g., DbUp) later

---

**Last Updated:** 2026-03-17 (migration from local to cloud)
