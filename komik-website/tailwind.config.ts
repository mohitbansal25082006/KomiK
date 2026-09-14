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
          cyan: "#00C2FF",
          magenta: "#FF1F6D",
          yellow: "#FFD700",
          black: "#08080A",
        },
        cyan: {
          DEFAULT: "#00C2FF",
          hover: "#4DD6FF",
          glow: "rgba(0, 194, 255, 0.2)",
        },
        crimson: {
          DEFAULT: "#FF1F6D",
          hover: "#FF4D8B",
        },
        magenta: {
          DEFAULT: "#FF1F6D",
          hover: "#FF4D8B",
        },
        violet: {
          ink: "#1B1030",
          deep: "#120B24",
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
        "comic-cyan": "5px 5px 0px #00C2FF",
        "comic-magenta": "5px 5px 0px #FF1F6D",
      },
      fontFamily: {
        display: ["var(--font-space-grotesk)", "system-ui", "sans-serif"],
        heading: ["var(--font-space-grotesk)", "sans-serif"],
        body: ["var(--font-jakarta)", "system-ui", "sans-serif"],
        mono: ["var(--font-jetbrains-mono)", "monospace"],
        bangers: ["var(--font-bangers)", "Impact", "sans-serif"],
        comic: ["var(--font-comic)", "Comic Sans MS", "cursive"],
      },
    },
  },
  plugins: [],
};

export default config;
