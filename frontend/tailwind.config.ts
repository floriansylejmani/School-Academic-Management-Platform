import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/features/**/*.{js,ts,jsx,tsx,mdx}"
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: "#f0f7ff",
          100: "#d9ebff",
          200: "#b9dbff",
          300: "#86c3ff",
          400: "#4aa3ff",
          500: "#1c82f2",
          600: "#0d67d1",
          700: "#0d53a9",
          800: "#114686",
          900: "#153d70",
          950: "#102645"
        },
        sand: {
          50: "#f8f6ef",
          100: "#efe8d5",
          200: "#e0d2ad",
          300: "#d0bb81"
        }
      },
      boxShadow: {
        sm: "0 1px 3px rgba(17, 29, 52, 0.06), 0 1px 2px rgba(17, 29, 52, 0.04)",
        card: "0 4px 12px rgba(17, 29, 52, 0.06), 0 1px 3px rgba(17, 29, 52, 0.04)",
        panel: "0 20px 45px rgba(17, 29, 52, 0.08)",
        "panel-lg": "0 32px 64px rgba(17, 29, 52, 0.12)",
        ring: "0 0 0 3px rgba(28, 130, 242, 0.18)"
      },
      backgroundImage: {
        "dashboard-glow":
          "radial-gradient(circle at top left, rgba(28,130,242,0.18), transparent 30%), radial-gradient(circle at bottom right, rgba(224,210,173,0.26), transparent 28%)"
      }
    }
  },
  plugins: []
};

export default config;
