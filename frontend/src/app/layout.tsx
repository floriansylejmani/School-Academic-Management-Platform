import type { Metadata } from "next";
import { Providers } from "@/lib/providers";
import "@/app/globals.css";

export const metadata: Metadata = {
  title: "Scholara — Academic Management Platform",
  description: "Unified platform for student enrolment, attendance, assessments, and institutional finance."
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
