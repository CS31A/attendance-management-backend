# Schedule Unique Constraint Migration Notes

## Migration Overview
**Date:** April 27, 2026  
**Migrations:** 
- `20260427142630_FixScheduleUniqueConstraint` - Primary EF Core migration
- `20260427150000_ForceFixScheduleIndex` - Defensive SQL force fix

## Problem Statement
The original unique constraint on `(TimeIn, TimeOut)` was too restrictive. It prevented ANY two schedules from having the same time range globally, even if they were in different classrooms or on different days.

For example:
- Room A, Monday, 9:00-10:00 AM ✓
- Room B, Monday, 9:00-10:00 AM ✗ (rejected by old constraint - incorrect)
- Room A, Tuesday, 9:00-10:00 AM ✗ (rejected by old constraint - incorrect)

## Solution
Changed unique constraint to `(ClassroomId, DayOfWeek, TimeIn, TimeOut)`. This properly prevents conflicts only within the same classroom on the same day.

Now:
- Room A, Monday, 9:00-10:00 AM ✓
- Room B, Monday, 9:00-10:00 AM ✓ (different classroom)
- Room A, Tuesday, 9:00-10:00 AM ✓ (different day)
- Room A, Monday, 9:00-10:00 AM ✗ (duplicate - correct)

## Why Two Migrations?

### Primary Migration (`20260427142630_FixScheduleUniqueConstraint`)
Standard EF Core migration that:
- Drops old index `IX_Schedules_TimeIn_TimeOut`
- Creates new composite unique index `IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut`

### Force Fix Migration (`20260427150000_ForceFixScheduleIndex`)
Defensive SQL migration with conditional checks that:
- Uses raw SQL with `IF EXISTS` / `IF NOT EXISTS` guards
- Designed to handle edge cases where the primary migration might fail or partially apply
- Idempotent - safe to run multiple times

**Rationale:** The force fix migration was added to handle potential deployment scenarios where:
1. The primary migration fails mid-execution
2. The old index already exists in an unexpected state
3. Manual intervention was previously required to fix index state
4. Production databases might have inconsistent index states

## Pre-Deployment Checklist

- [ ] Run `verify-schedule-constraint.sql` on production database to check for existing data violations
- [ ] If duplicates found, resolve them before deploying
- [ ] Test migrations on a staging/copy of production database
- [ ] Verify both migrations run successfully: `dotnet ef database update`
- [ ] Test schedule creation with same time in different classrooms (should succeed)
- [ ] Test schedule creation with same time in same classroom/day (should fail)

## Rollback Plan
If issues occur after deployment:

```bash
# Rollback to previous migration
dotnet ef database update 20260427142630_FixScheduleUniqueConstraint

# Or if force fix was applied, rollback further
dotnet ef database update <previous-migration-before-fix>
```

The force fix migration includes a proper Down() method that reverses the changes.

## Post-Deployment Cleanup
After successful deployment and verification:
- Consider whether the force fix migration is still needed
- If the primary migration works reliably in production, the force fix can be removed in a future cleanup migration
- Document any issues encountered during deployment for future reference
