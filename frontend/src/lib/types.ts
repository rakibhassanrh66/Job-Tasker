// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

/**
 * Hand-written mirrors of the API's DTOs.
 *
 * The API serialises enums as integers — no JsonStringEnumConverter is registered — so
 * these are numeric unions matching the C# values exactly. The backing values start at 1
 * on purpose there: a default of 0 is not a valid role, so an unset field can never read
 * as Admin. The label maps below are the only place a number becomes text for display.
 *
 * Property names are camelCase because ASP.NET Core's default JSON policy camelCases
 * them on the way out, even though the C# records declare them in PascalCase.
 */

// ---------------------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------------------

export const UserRole = {
  Admin: 1,
  Teacher: 2,
  Student: 3,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const AssignmentStatus = {
  Draft: 1,
  Published: 2,
  Archived: 3,
} as const;

export type AssignmentStatus = (typeof AssignmentStatus)[keyof typeof AssignmentStatus];

export const SubmissionStatus = {
  Submitted: 1,
  UnderReview: 2,
  Graded: 3,
  Returned: 4,
  Late: 5,
} as const;

export type SubmissionStatus = (typeof SubmissionStatus)[keyof typeof SubmissionStatus];

export const roleLabels: Record<UserRole, string> = {
  [UserRole.Admin]: "Admin",
  [UserRole.Teacher]: "Teacher",
  [UserRole.Student]: "Student",
};

export const assignmentStatusLabels: Record<AssignmentStatus, string> = {
  [AssignmentStatus.Draft]: "Draft",
  [AssignmentStatus.Published]: "Published",
  [AssignmentStatus.Archived]: "Archived",
};

export const submissionStatusLabels: Record<SubmissionStatus, string> = {
  [SubmissionStatus.Submitted]: "Submitted",
  [SubmissionStatus.UnderReview]: "Under review",
  [SubmissionStatus.Graded]: "Graded",
  [SubmissionStatus.Returned]: "Returned",
  [SubmissionStatus.Late]: "Late",
};

/** Home route per role. Used by the login redirect and by the guard when a role lands
 *  somewhere it should not be. */
export const roleHome: Record<UserRole, string> = {
  [UserRole.Admin]: "/admin/users",
  [UserRole.Teacher]: "/teacher/assignments",
  [UserRole.Student]: "/student/assignments",
};

// ---------------------------------------------------------------------------------
// Paging
// ---------------------------------------------------------------------------------

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

// ---------------------------------------------------------------------------------
// Auth
// ---------------------------------------------------------------------------------

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserProfile;
}

// ---------------------------------------------------------------------------------
// Admin
// ---------------------------------------------------------------------------------

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface ClassCourseDto {
  id: string;
  name: string;
  code: string;
  subjectCount: number;
  enrollmentCount: number;
}

export interface SubjectDto {
  id: string;
  name: string;
  code: string;
  classCourseId: string;
  classCourseName: string;
  classCourseCode: string;
}

export interface TeacherAssignmentDto {
  id: string;
  teacherId: string;
  teacherName: string;
  subjectId: string;
  subjectName: string;
  classCourseId: string;
  classCourseCode: string;
}

export interface EnrollmentDto {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  classCourseId: string;
  classCourseCode: string;
  createdAt: string;
}

// ---------------------------------------------------------------------------------
// Assignments
// ---------------------------------------------------------------------------------

/** What a teacher or an admin sees. Carries the submission count and the authoring
 *  teacher, neither of which reaches a student. */
export interface AssignmentDto {
  id: string;
  title: string;
  description: string;
  deadline: string;
  maxMarks: number;
  status: AssignmentStatus;
  classCourseId: string;
  classCourseCode: string;
  subjectId: string;
  subjectName: string;
  createdByTeacherId: string;
  createdByTeacherName: string;
  allowLateSubmission: boolean;
  allowUpdateBeforeDeadline: boolean;
  submissionCount: number;
  createdAt: string;
  updatedAt: string;
}

/** What a student sees: their own submission state instead of the aggregate. */
export interface StudentAssignmentDto {
  id: string;
  title: string;
  description: string;
  deadline: string;
  maxMarks: number;
  classCourseId: string;
  classCourseCode: string;
  subjectId: string;
  subjectName: string;
  teacherName: string;
  allowLateSubmission: boolean;
  allowUpdateBeforeDeadline: boolean;
  hasSubmitted: boolean;
  submissionId: string | null;
  submissionStatus: SubmissionStatus | null;
  marks: number | null;
}

// ---------------------------------------------------------------------------------
// Submissions
// ---------------------------------------------------------------------------------

export interface SubmissionDto {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  answerText: string;
  attachmentUrl: string | null;
  status: SubmissionStatus;
  submittedAt: string;
  updatedAt: string;
  marks: number | null;
  /** Carried alongside marks so a client renders "18 / 20" without fetching the parent. */
  maxMarks: number;
  feedback: string | null;
  gradedByTeacherId: string | null;
  gradedByTeacherName: string | null;
  gradedAt: string | null;
}
