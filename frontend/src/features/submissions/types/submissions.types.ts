export type SubmissionAiMode = "Feedback" | "SmartGrade";

export interface RubricBreakdownItem {
  criterion: string;
  score: number;
  maxScore: number;
  feedback: string;
}

export interface SubmissionAIReview {
  mode: SubmissionAiMode;
  model: string;
  generatedAt: string;
  grammarScore: number;
  clarityScore: number;
  structureScore: number;
  contentScore: number;
  overallSuggestedScore: number;
  summaryFeedback: string;
  strengths: string[];
  weaknesses: string[];
  improvements: string[];
  rubricBreakdown: RubricBreakdownItem[];
  safetyNotes?: string | null;
}

export interface Submission {
  id: string;
  examId: string;
  examTitle: string;
  studentId: string;
  studentName: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  essayPrompt?: string | null;
  answerText: string;
  maximumScore: number;
  teacherFinalScore?: number | null;
  teacherFinalGrade?: string | null;
  teacherReviewNotes?: string | null;
  isAiFeedbackReleasedToStudent: boolean;
  submittedAt: string;
  reviewedAt?: string | null;
  hasAIReview: boolean;
  aiReview?: SubmissionAIReview | null;
}

export interface CreateSubmissionDto {
  examId: string;
  studentId?: string | null;
  essayPrompt?: string | null;
  answerText: string;
}

export interface RequestSubmissionAIDto {
  rubricInstructions?: string | null;
  additionalInstructions?: string | null;
}

export interface UpdateSubmissionTeacherReviewDto {
  teacherFinalScore?: number | null;
  teacherFinalGrade?: string | null;
  teacherReviewNotes?: string | null;
  isAiFeedbackReleasedToStudent: boolean;
}

export interface SubmissionFilterParams {
  examId?: string;
  studentId?: string;
  releasedOnly?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
