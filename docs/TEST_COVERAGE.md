# Test coverage — business rule → test

> Authored by Rakib Hassan · submitted for candidacy evaluation · see [LICENSE](../LICENSE).

Which test proves which rule, so a reviewer can check a claim without reading the whole suite.

Run everything:

```bash
dotnet test backend/AssignmentSystem.sln
```

Run one rule's tests:

```bash
dotnet test backend/AssignmentSystem.sln --filter "FullyQualifiedName~StudentModuleTests"
```

Integration tests need Docker running — `ApiFactory` starts a throwaway `postgres:16`
container per run via Testcontainers, so there is no local database to configure.

**Layers.** Unit tests cover the pure policies and the authorizer with no database.
Integration tests drive the real application over HTTP through `WebApplicationFactory`, so
what they observe is what a caller observes — the real pipeline, real authentication, real
middleware. Where a rule is enforced in more than one place, it is listed against each.

---

## The eleven business rules

### Rule 1 — Students never see Draft or Archived assignments

| Test | File |
|---|---|
| `Student_Cannot_See_Draft_Assignments` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Cannot_See_Archived_Assignments` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Requesting_Draft_By_Id_Returns_404` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Only_Published_Assignments_Are_Visible_To_Students` | `UnitTests/Assignments/StatusPolicyTests.cs` |

A draft answers **404, not 403**. A 403 would confirm that an assignment with that id is
being prepared, which is exactly what hiding drafts is for.

### Rule 2 — A student only reaches assignments for classes they are enrolled in

| Test | File |
|---|---|
| `Student_Only_Sees_Assignments_For_Enrolled_Class` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Requesting_Other_Class_Assignment_By_Id_Returns_403` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Cannot_Submit_To_Assignment_Of_Unenrolled_Class_Returns_403` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Enrolled_Student_Is_Allowed` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Student_Not_Enrolled_In_The_Class_Is_Forbidden` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Another_Students_Enrolment_Does_Not_Grant_Access` | `UnitTests/Security/ResourceAuthorizerTests.cs` |

The seed puts a published assignment in MATH-201 and enrols `student@demo.test` in CS-101
only, so "a class the student is not in" is a real fixture rather than a hypothetical.

### Rule 3 — A teacher may only create assignments where they are allocated

| Test | File |
|---|---|
| `Teacher_Can_Create_Assignment_For_Assigned_SubjectClass` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Cannot_Create_Assignment_For_Unassigned_SubjectClass_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `A_Teacher_Cannot_Set_Work_In_A_Subject_They_Are_Not_Allocated_To` | `IntegrationTests/Workflow/EndToEndWorkflowTests.cs` |
| `Teacher_Allocated_To_The_Subject_And_Class_Is_Allowed` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Teacher_Not_Allocated_To_The_Pair_Is_Forbidden` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Teaching_The_Subject_In_A_Different_Class_Is_Not_Enough` | `UnitTests/Security/ResourceAuthorizerTests.cs` |

The allocation is on the **pair** (subject, class), not on either alone — the third unit test
is the one that pins that down.

### Rule 4 — A teacher may only act on assignments they created

| Test | File |
|---|---|
| `Teacher_Cannot_Update_Another_Teachers_Assignment_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Cannot_Delete_Another_Teachers_Assignment_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Cannot_Publish_Another_Teachers_Assignment_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Cannot_List_Submissions_Of_Another_Teachers_Assignment_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Cannot_Grade_Submission_Of_Another_Teachers_Assignment_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Fetching_Another_Teachers_Assignment_By_Id_Returns_403` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Can_Fetch_Their_Own_Assignment_By_Id` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Only_Sees_Their_Own_Assignments_In_Mine` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Teacher_Who_Created_The_Assignment_Is_Allowed` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Teacher_Who_Did_Not_Create_The_Assignment_Is_Forbidden` | `UnitTests/Security/ResourceAuthorizerTests.cs` |

Covers reads as well as writes. `teacher2@demo.test` teaches only in MATH-201, so there is
always a teacher who provably does not own a CS-101 assignment.

### Rule 5 — The deadline, and late submission

| Test | File |
|---|---|
| `Submit_Before_Deadline_Succeeds_With_Status_Submitted` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Submit_After_Deadline_Without_LateAllowed_Returns_409` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Submit_After_Deadline_With_LateAllowed_Creates_Submission_With_Status_Late` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Before_The_Deadline_Is_Not_Past` | `UnitTests/Domain/AssignmentDeadlineTests.cs` |
| `After_The_Deadline_Is_Past` | `UnitTests/Domain/AssignmentDeadlineTests.cs` |
| `Exactly_On_The_Deadline_Is_Not_Past` | `UnitTests/Domain/AssignmentDeadlineTests.cs` |

Late work is accepted where the assignment allows it, but permanently marked `Late` rather
than `Submitted` — the boundary case (`Exactly_On_The_Deadline`) is where an off-by-one hides.

### Rule 6 — One submission per student per assignment

| Test | File |
|---|---|
| `Second_Submit_To_Same_Assignment_Returns_409` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Duplicate_Submission_Violates_Unique_Index` | `IntegrationTests/Persistence/DatabaseSchemaTests.cs` |

Enforced twice on purpose. The service checks first for a clean 409; the unique index on
`(AssignmentId, StudentId)` is the guarantee that survives a race between two requests.

### Rule 7 — The update window

| Test | File |
|---|---|
| `Update_Before_Deadline_When_Allowed_Succeeds` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Update_When_AllowUpdateBeforeDeadline_False_Returns_409` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Update_After_Deadline_Returns_409` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Update_After_Grading_Returns_409` | `IntegrationTests/Student/StudentModuleTests.cs` |

Three independent ways the window closes: the assignment forbids updates, the deadline
passes, or a teacher has already graded the work. The last is the one where every other
axis is still open, so it isolates that condition — marks must always describe the content
that was graded.

### Rule 8 — A student owns only their own submission

| Test | File |
|---|---|
| `Student_Cannot_Read_Another_Students_Submission_Returns_403` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Cannot_Update_Another_Students_Submission_Returns_403` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `MySubmissions_Returns_Only_Own_Submissions_With_Marks_And_Feedback` | `IntegrationTests/Student/StudentModuleTests.cs` |
| `Student_Who_Owns_The_Submission_Is_Allowed` | `UnitTests/Security/ResourceAuthorizerTests.cs` |
| `Student_Reaching_Another_Students_Submission_Is_Forbidden` | `UnitTests/Security/ResourceAuthorizerTests.cs` |

Ownership is checked **before** the update window, so a student cannot learn whether another
student's submission is still editable.

### Rule 9 — Marks fall within [0, MaxMarks]

| Test | File |
|---|---|
| `Grade_With_Negative_Marks_Returns_422` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grade_With_Marks_Above_MaxMarks_Returns_422` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grade_With_Marks_Equal_To_MaxMarks_Succeeds` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grade_With_Zero_Marks_Succeeds` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Negative_Marks_Violates_Check_Constraint` | `IntegrationTests/Persistence/DatabaseSchemaTests.cs` |
| `MaxMarks_Must_Be_Positive` | `IntegrationTests/Persistence/DatabaseSchemaTests.cs` |

Both boundaries are tested as successes, not just the failures either side. The lower bound
is a real database `CHECK`; the upper bound lives in the service because a PostgreSQL check
constraint on `Submission` cannot reference `Assignment.MaxMarks`.

### Rule 10 — Submission status transitions

| Test | File |
|---|---|
| `Status_Transitions_Are_Enforced` (8 cases) | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grade_Endpoint_Accepts_Submitted_UnderReview_And_Late` (3 cases) | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grading_An_Already_Graded_Submission_Returns_409` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Grading_Sets_GradedByTeacherId_And_GradedAt` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Only_The_Listed_Transitions_Are_Permitted` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Nothing_Transitions_Into_Late` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `No_Status_Transitions_To_Itself` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Returned_Is_Terminal` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Grading_Is_Allowed_From_The_Pre_Grade_States` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Grading_An_Already_Graded_Submission_Is_Refused` | `UnitTests/Assignments/StatusPolicyTests.cs` |

The unit tests enumerate the whole transition table rather than sampling it, so a transition
added by accident fails a test.

### Rule 11 — Assignment publish transitions

| Test | File |
|---|---|
| `Publish_Draft_Assignment_Succeeds` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Publish_Already_Published_Returns_409` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Publish_Archived_Assignment_Returns_409` | `IntegrationTests/Teacher/TeacherModuleTests.cs` |
| `Publishing_A_Draft_Is_Allowed` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Republishing_A_Published_Assignment_Is_Refused` | `UnitTests/Assignments/StatusPolicyTests.cs` |
| `Publishing_An_Archived_Assignment_Is_Refused` | `UnitTests/Assignments/StatusPolicyTests.cs` |

Re-publishing is a 409 rather than a silent no-op: the caller should learn that nothing
changed.

---

## Authorization

### The role gate, across every route

| Test | File |
|---|---|
| `Endpoint_Enforces_Its_Role_Gate` (41 routes) | `IntegrationTests/Security/AuthorizationMatrixTests.cs` |

One table covering every endpoint in the API. For each: no token → 401, wrong role → 403,
right role → neither. This is the test that catches a route added without a role attribute,
which per-module suites cannot.

### Role gates on the admin surface

| Test | File |
|---|---|
| `Teacher_Hitting_Admin_Route_Returns_403` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Student_Hitting_Admin_Route_Returns_403` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Unauthenticated_Hitting_Admin_Route_Returns_401` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Teacher_Cannot_Allocate_Themselves_To_A_Subject` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Student_Cannot_Enrol_Themselves_In_A_Class` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Teacher_Cannot_Create_A_User` / `_A_Class` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |
| `Student_Cannot_Delete_A_Class` | `IntegrationTests/Admin/AdminRoleGateTests.cs` |

Allocation and enrolment are admin-only because they are the **inputs** to rules 3 and 2. A
teacher who could edit allocations could grant themselves permission to set work anywhere.

### Authentication and tokens

`UnitTests/Auth/AuthServiceTests.cs` and `IntegrationTests/Auth/AuthEndpointTests.cs` cover
login, refresh-token rotation and reuse detection (`Replaying_A_Revoked_Token_Revokes_The_Whole_Chain`),
identical failure messages for unknown email and wrong password, deactivated users, expired
and wrong-key tokens, and that no response ever carries a password hash, stack trace or
connection string. `IntegrationTests/Auth/LoginRateLimitTests.cs` covers the 429 on the
credential endpoints.

---

## Everything composed

| Test | File |
|---|---|
| `Admin_Provisions_Teacher_Assigns_Student_Submits_Teacher_Grades` | `IntegrationTests/Workflow/EndToEndWorkflowTests.cs` |

Builds a class, subject, teacher, student, allocation and enrolment through the API, then
walks draft → publish → submit → grade → student reads the mark back. Touches no DbContext:
if a step needed to reach behind the API, an evaluator following the README could not do it
either.

---

## Supporting coverage

- **Schema** — `IntegrationTests/Persistence/DatabaseSchemaTests.cs`: migrations create every
  table, and the unique indexes and check constraints actually bite.
- **Seeding** — `IntegrationTests/Persistence/DbSeederTests.cs`: idempotent across restarts,
  passwords stored hashed, demo accounts exist for all three roles, and the fixtures the
  business rules depend on are present.
- **API contract** — `IntegrationTests/Api/SwaggerTests.cs`: the document is served, declares
  bearer auth, documents both role-shapes of `GET /assignments/{id}`, and does not advertise
  filters a route cannot honour.
- **Filter contracts** — `Available_Rejects_A_Status_Filter_With_400`,
  `MySubmissions_Rejects_A_StudentId_Filter_With_400`,
  `Available_Still_Accepts_The_Filters_It_Does_Support` in `StudentModuleTests.cs`.

---

<sub>© 2026 Rakib Hassan · Evaluation build — not licensed for production use · sig:a24a5edb253940aa</sub>
