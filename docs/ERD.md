<!--
Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa
-->

# Entity Relationship Diagram

> **Status: stub.** Filled in during M1 (Domain + EF Core) with the full Mermaid diagram,
> the complete field list per entity, and the constraint/index table.

## Entities (planned)

| Entity | Purpose |
|---|---|
| `User` | Admin / Teacher / Student, distinguished by the `Role` enum |
| `ClassCourse` | A class or course |
| `Subject` | A subject taught within a `ClassCourse` |
| `Enrollment` | Which class a student belongs to |
| `TeacherAssignment` | Which subject/class a teacher teaches (set by Admin) |
| `Assignment` | An assignment created by a teacher for a subject + class |
| `Submission` | A student's submission for an assignment |
| `RefreshToken` | Stored/rotated refresh tokens — **addition to the Section 5 model**, required by Section 7's "stored/rotated" requirement |

## Relationships

```
ClassCourse 1───* Subject
ClassCourse 1───* Enrollment *───1 User(Student)
User(Teacher) *───* Subject   via TeacherAssignment (scoped to a ClassCourse)
Assignment *───1 ClassCourse
Assignment *───1 Subject
Assignment *───1 User(Teacher, CreatedBy)
Submission *───1 Assignment
Submission *───1 User(Student)      [UNIQUE (AssignmentId, StudentId)]
User        1───* RefreshToken
```

## Note on the `Marks` constraint

The build plan specifies a DB check of
`Marks IS NULL OR (Marks >= 0 AND Marks <= <maxMarks>)`.

A PostgreSQL `CHECK` constraint cannot reference a column in another table, and `MaxMarks`
lives on `Assignment`, not `Submission`. The bound is therefore split:

- **Database:** `CHECK (Marks IS NULL OR Marks >= 0)`
- **Service layer:** upper bound validated against the parent `Assignment.MaxMarks`, returning
  **422** when exceeded

This is documented in `README.md` under Assumptions.
