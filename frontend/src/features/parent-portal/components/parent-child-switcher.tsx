"use client";

import { Select } from "@/components/ui/select";
import type { Student } from "@/features/students/types/student.types";

interface ParentChildSwitcherProps {
  students: Student[];
  value: string | undefined;
  onChange: (childId: string) => void;
  className?: string;
}

export function ParentChildSwitcher({
  students,
  value,
  onChange,
  className = "w-64"
}: ParentChildSwitcherProps) {
  if (students.length <= 1) {
    return null;
  }

  return (
    <Select value={value ?? ""} onChange={(event) => onChange(event.target.value)} className={className}>
      {students.map((child) => (
        <option key={child.id} value={child.id}>
          {child.fullName}{child.className ? ` - ${child.className}` : ""}
        </option>
      ))}
    </Select>
  );
}
