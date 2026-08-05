// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Security.Cryptography;
using System.Text;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Infrastructure.Persistence;

/// <summary>
/// Populates demo data so an evaluator can log in immediately after `docker compose up`
/// without touching SQL.
///
/// Idempotent: every row gets a deterministic id derived from a stable natural key, and
/// nothing is inserted if that id already exists. Running the seeder repeatedly — which
/// happens on every API start — leaves row counts unchanged.
/// </summary>
public static class DbSeeder
{
    // Demo passwords. Not secrets: throwaway credentials for evaluation, documented in
    // the README, hashed before they ever reach the database.
    public const string AdminPassword = "Admin@123";
    public const string TeacherPassword = "Teacher@123";
    public const string StudentPassword = "Student@123";

    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var users = await SeedUsersAsync(db, passwordHasher, now, cancellationToken);
        var classes = await SeedClassesAsync(db, cancellationToken);
        var subjects = await SeedSubjectsAsync(db, classes, cancellationToken);
        await SeedTeacherAssignmentsAsync(db, users, subjects, classes, now, cancellationToken);
        await SeedEnrollmentsAsync(db, users, classes, now, cancellationToken);
        var assignments = await SeedAssignmentsAsync(db, users, subjects, classes, now, cancellationToken);
        await SeedSubmissionsAsync(db, users, assignments, now, cancellationToken);

        var written = await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            written == 0
                ? "Seeder ran; database already populated, no changes written."
                : "Seeder ran; {Count} row(s) written.",
            written);
    }

    // ---------------------------------------------------------------------------------
    // Users
    // ---------------------------------------------------------------------------------

    private static async Task<Dictionary<string, User>> SeedUsersAsync(
        AppDbContext db, IPasswordHasher hasher, DateTime now, CancellationToken ct)
    {
        var wanted = new (string Email, string FullName, UserRole Role, string Password)[]
        {
            ("admin@demo.test",    "Ayesha Rahman",   UserRole.Admin,   AdminPassword),
            ("teacher@demo.test",  "Imran Chowdhury", UserRole.Teacher, TeacherPassword),
            ("teacher2@demo.test", "Nusrat Jahan",    UserRole.Teacher, TeacherPassword),
            ("student@demo.test",  "Tanvir Ahmed",    UserRole.Student, StudentPassword),
            ("student2@demo.test", "Sadia Islam",     UserRole.Student, StudentPassword),
            ("student3@demo.test", "Rafiq Hossain",   UserRole.Student, StudentPassword),
            ("student4@demo.test", "Mitu Akter",      UserRole.Student, StudentPassword)
        };

        var ids = wanted.Select(w => DeterministicId($"user:{w.Email}")).ToList();
        var existing = await db.Users.Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var result = new Dictionary<string, User>();

        foreach (var (email, fullName, role, password) in wanted)
        {
            var id = DeterministicId($"user:{email}");

            if (existing.TryGetValue(id, out var found))
            {
                result[email] = found;
                continue;
            }

            var user = new User
            {
                Id = id,
                Email = email,
                FullName = fullName,
                Role = role,
                IsActive = true,
                CreatedAt = now,
                PasswordHash = hasher.Hash(password)
            };

            db.Users.Add(user);
            result[email] = user;
        }

        return result;
    }

    // ---------------------------------------------------------------------------------
    // Classes and subjects
    // ---------------------------------------------------------------------------------

    private static async Task<Dictionary<string, ClassCourse>> SeedClassesAsync(
        AppDbContext db, CancellationToken ct)
    {
        var wanted = new (string Code, string Name)[]
        {
            ("CS-101",   "Computer Science 101"),
            ("MATH-201", "Mathematics 201")
        };

        var ids = wanted.Select(w => DeterministicId($"class:{w.Code}")).ToList();
        var existing = await db.ClassCourses.Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var result = new Dictionary<string, ClassCourse>();

        foreach (var (code, name) in wanted)
        {
            var id = DeterministicId($"class:{code}");

            if (existing.TryGetValue(id, out var found))
            {
                result[code] = found;
                continue;
            }

            var entity = new ClassCourse { Id = id, Code = code, Name = name };
            db.ClassCourses.Add(entity);
            result[code] = entity;
        }

        return result;
    }

    private static async Task<Dictionary<string, Subject>> SeedSubjectsAsync(
        AppDbContext db, Dictionary<string, ClassCourse> classes, CancellationToken ct)
    {
        // Order is deliberate and fixed, not alphabetical.
        var wanted = new (string Code, string Name, string ClassCode)[]
        {
            ("DS-101",  "Data Structures", "CS-101"),
            ("ALG-101", "Algorithms",      "CS-101"),
            ("LA-201",  "Linear Algebra",  "MATH-201"),
            ("CAL-201", "Calculus",        "MATH-201")
        };

        var ids = wanted.Select(w => DeterministicId($"subject:{w.Code}")).ToList();
        var existing = await db.Subjects.Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var result = new Dictionary<string, Subject>();

        foreach (var (code, name, classCode) in wanted)
        {
            var id = DeterministicId($"subject:{code}");

            if (existing.TryGetValue(id, out var found))
            {
                result[code] = found;
                continue;
            }

            var entity = new Subject
            {
                Id = id,
                Code = code,
                Name = name,
                ClassCourseId = classes[classCode].Id
            };

            db.Subjects.Add(entity);
            result[code] = entity;
        }

        return result;
    }

    // ---------------------------------------------------------------------------------
    // Teaching allocations and enrolments
    // ---------------------------------------------------------------------------------

    private static async Task SeedTeacherAssignmentsAsync(
        AppDbContext db,
        Dictionary<string, User> users,
        Dictionary<string, Subject> subjects,
        Dictionary<string, ClassCourse> classes,
        DateTime now,
        CancellationToken ct)
    {
        // Note that teacher2 teaches only in MATH-201. That gap is deliberate: it gives
        // the tests a teacher who provably does not teach CS-101, which is what business
        // rule 3 needs in order to be tested for rejection rather than only acceptance.
        var wanted = new (string TeacherEmail, string SubjectCode, string ClassCode)[]
        {
            ("teacher@demo.test",  "DS-101",  "CS-101"),
            ("teacher@demo.test",  "ALG-101", "CS-101"),
            ("teacher2@demo.test", "LA-201",  "MATH-201")
        };

        var ids = wanted
            .Select(w => DeterministicId($"ta:{w.TeacherEmail}:{w.SubjectCode}:{w.ClassCode}"))
            .ToList();

        var existing = await db.TeacherAssignments.Where(t => ids.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var (teacherEmail, subjectCode, classCode) in wanted)
        {
            var id = DeterministicId($"ta:{teacherEmail}:{subjectCode}:{classCode}");
            if (existing.Contains(id))
            {
                continue;
            }

            db.TeacherAssignments.Add(new TeacherAssignment
            {
                Id = id,
                TeacherId = users[teacherEmail].Id,
                SubjectId = subjects[subjectCode].Id,
                ClassCourseId = classes[classCode].Id,
                CreatedAt = now
            });
        }
    }

    private static async Task SeedEnrollmentsAsync(
        AppDbContext db,
        Dictionary<string, User> users,
        Dictionary<string, ClassCourse> classes,
        DateTime now,
        CancellationToken ct)
    {
        // Split across both classes so class-scoping (business rule 2) has a student who
        // must NOT see the other class's assignments.
        var wanted = new (string StudentEmail, string ClassCode)[]
        {
            ("student@demo.test",  "CS-101"),
            ("student2@demo.test", "CS-101"),
            ("student3@demo.test", "MATH-201"),
            ("student4@demo.test", "MATH-201")
        };

        var ids = wanted.Select(w => DeterministicId($"enr:{w.StudentEmail}:{w.ClassCode}")).ToList();
        var existing = await db.Enrollments.Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(ct);

        foreach (var (studentEmail, classCode) in wanted)
        {
            var id = DeterministicId($"enr:{studentEmail}:{classCode}");
            if (existing.Contains(id))
            {
                continue;
            }

            db.Enrollments.Add(new Enrollment
            {
                Id = id,
                StudentId = users[studentEmail].Id,
                ClassCourseId = classes[classCode].Id,
                CreatedAt = now
            });
        }
    }

    // ---------------------------------------------------------------------------------
    // Assignments and submissions
    // ---------------------------------------------------------------------------------

    private static async Task<Dictionary<string, Assignment>> SeedAssignmentsAsync(
        AppDbContext db,
        Dictionary<string, User> users,
        Dictionary<string, Subject> subjects,
        Dictionary<string, ClassCourse> classes,
        DateTime now,
        CancellationToken ct)
    {
        var wanted = new (string Key, string Title, string Description, DateTime Deadline,
            int MaxMarks, AssignmentStatus Status, string SubjectCode, string ClassCode,
            string TeacherEmail, bool AllowLate, bool AllowUpdate)[]
        {
            ("ds-linked-list",
                "Implement a singly linked list",
                "Implement insert, delete and reverse. Include complexity analysis for each.",
                now.AddDays(14), 100, AssignmentStatus.Published, "DS-101", "CS-101",
                "teacher@demo.test", false, true),

            ("alg-sorting-draft",
                "Comparison of sorting algorithms",
                "Not yet released to students — exists so draft visibility can be demonstrated.",
                now.AddDays(21), 50, AssignmentStatus.Draft, "ALG-101", "CS-101",
                "teacher@demo.test", false, true),

            ("ds-recursion-late",
                "Recursion exercises",
                "Deadline has already passed, but late submissions are accepted and flagged.",
                now.AddDays(-7), 40, AssignmentStatus.Published, "DS-101", "CS-101",
                "teacher@demo.test", true, false),

            ("la-matrices",
                "Matrix operations worksheet",
                "Belongs to MATH-201, so CS-101 students must not see it.",
                now.AddDays(10), 60, AssignmentStatus.Published, "LA-201", "MATH-201",
                "teacher2@demo.test", false, true)
        };

        var ids = wanted.Select(w => DeterministicId($"asg:{w.Key}")).ToList();
        var existing = await db.Assignments.Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var result = new Dictionary<string, Assignment>();

        foreach (var w in wanted)
        {
            var id = DeterministicId($"asg:{w.Key}");

            if (existing.TryGetValue(id, out var found))
            {
                result[w.Key] = found;
                continue;
            }

            var entity = new Assignment
            {
                Id = id,
                Title = w.Title,
                Description = w.Description,
                Deadline = w.Deadline,
                MaxMarks = w.MaxMarks,
                Status = w.Status,
                SubjectId = subjects[w.SubjectCode].Id,
                ClassCourseId = classes[w.ClassCode].Id,
                CreatedByTeacherId = users[w.TeacherEmail].Id,
                AllowLateSubmission = w.AllowLate,
                AllowUpdateBeforeDeadline = w.AllowUpdate,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Assignments.Add(entity);
            result[w.Key] = entity;
        }

        return result;
    }

    private static async Task SeedSubmissionsAsync(
        AppDbContext db,
        Dictionary<string, User> users,
        Dictionary<string, Assignment> assignments,
        DateTime now,
        CancellationToken ct)
    {
        var wanted = new (string AssignmentKey, string StudentEmail, string AnswerText,
            SubmissionStatus Status, int? Marks, string? Feedback, string? GradedByEmail)[]
        {
            ("ds-linked-list", "student@demo.test",
                "Implementation attached. Reverse is iterative, O(n) time and O(1) space.",
                SubmissionStatus.Submitted, null, null, null),

            ("ds-linked-list", "student2@demo.test",
                "Implemented all three operations with unit tests for the edge cases.",
                SubmissionStatus.Graded, 92, "Clear write-up and correct complexity analysis.",
                "teacher@demo.test")
        };

        var ids = wanted
            .Select(w => DeterministicId($"sub:{w.AssignmentKey}:{w.StudentEmail}"))
            .ToList();

        var existing = await db.Submissions.Where(s => ids.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var w in wanted)
        {
            var id = DeterministicId($"sub:{w.AssignmentKey}:{w.StudentEmail}");
            if (existing.Contains(id))
            {
                continue;
            }

            db.Submissions.Add(new Submission
            {
                Id = id,
                AssignmentId = assignments[w.AssignmentKey].Id,
                StudentId = users[w.StudentEmail].Id,
                AnswerText = w.AnswerText,
                Status = w.Status,
                SubmittedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1),
                Marks = w.Marks,
                Feedback = w.Feedback,
                GradedByTeacherId = w.GradedByEmail is null ? null : users[w.GradedByEmail].Id,
                GradedAt = w.GradedByEmail is null ? null : now
            });
        }
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Derives a stable GUID from a natural key, so re-seeding produces the same ids and
    /// "insert only if missing" is a simple primary-key check. SHA-256 truncated to 16
    /// bytes — used purely to spread values, never as a security primitive.
    /// </summary>
    private static Guid DeterministicId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"assignment-system:{key}"));
        return new Guid(hash.AsSpan(0, 16).ToArray());
    }
}
