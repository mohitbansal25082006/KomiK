import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: "#08080A",
        "ink-deep": "#040405",
        panel: "#121215",
        "panel-card": "#18181D",
        gutter: "#000000",
        "gutter-border": "#27272A",
        paper: {
          DEFAULT: "#F5EFEB",
          pure: "#FAF6F0",
          muted: "#E4DCD3",
          dark: "#16161A",
        },
        newsprint: "#F4EFEA",
        muted: "#949BA6",
        cmyk: {
          cyan: "#00A3E0",
          magenta: "#E60050",
          yellow: "#FFD700",
          black: "#08080A",
        },
        cyan: {
          DEFAULT: "#00A3E0",
          hover: "#00B8FC",
          glow: "rgba(0, 163, 224, 0.2)",
        },
        crimson: {
          DEFAULT: "#E60050",
          hover: "#FF1A6B",
        },
        amber: {
          DEFAULT: "#FFD700",
          hover: "#FFE233",
        },
      },
      boxShadow: {
        "comic-xs": "2px 2px 0px #000000",
        "comic-sm": "4px 4px 0px #000000",
        comic: "6px 6px 0px #000000",
        "comic-lg": "9px 9px 0px #000000",
        "comic-xl": "12px 12px 0px #000000",
        "comic-paper": "6px 6px 0px #000000",
        "comic-yellow": "5px 5px 0px #FFD700",
        "comic-cyan": "5px 5px 0px #00A3E0",
        "comic-magenta": "5px 5px 0px #E60050",
      },
      fontFamily: {
        display: ["var(--font-space-grotesk)", "system-ui", "sans-serif"],
        heading: ["var(--font-space-grotesk)", "sans-serif"],
        body: ["var(--font-jakarta)", "system-ui", "sans-serif"],
        mono: ["var(--font-jetbrains-mono)", "monospace"],
      },
    },
  },
  plugins: [],
};

export default config;
