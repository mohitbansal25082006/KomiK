/** KomiK Downloader design tokens: the same CMYK ink palette, lettering and hard shadows as the Komik website. */
module.exports = {
  content: ["./src/pages/**/*.html", "./src/ui/**/*.{ts,tsx}", "./src/pages/**/*.{ts,tsx}"],
  // Button tones are picked at runtime (btn-${tone}), so Tailwind must keep them.
  safelist: ["btn-yellow", "btn-cyan", "btn-magenta", "btn-ink"],
  darkMode: ["class", "html.dark"],
  theme: {
    extend: {
      colors: {
        ink: "rgb(var(--c-ink) / <alpha-value>)",
        "ink-deep": "rgb(var(--c-ink-deep) / <alpha-value>)",
        panel: "rgb(var(--c-panel) / <alpha-value>)",
        "panel-card": "rgb(var(--c-panel-card) / <alpha-value>)",
        line: "rgb(var(--c-line) / <alpha-value>)",
        text: "rgb(var(--c-text) / <alpha-value>)",
        muted: "rgb(var(--c-muted) / <alpha-value>)",
        gutter: "#08080A",
        paper: { DEFAULT: "#F5EFE3", pure: "#FFFDF6" },
        yellow: { DEFAULT: "#FFD700", hover: "#FFE233" },
        cyan: { DEFAULT: "#00C2FF", hover: "#4DD6FF" },
        magenta: { DEFAULT: "#FF1F6D", hover: "#FF4D8B" },
        orange: "#FF7A00",
        green: "#2FD17A",
        violet: "#A78BFA"
      },
      boxShadow: {
        "comic-xs": "2px 2px 0 #08080A",
        "comic-sm": "3px 3px 0 #08080A",
        comic: "5px 5px 0 #08080A",
        "comic-lg": "8px 8px 0 #08080A",
        "comic-yellow": "4px 4px 0 #FFD700",
        "comic-cyan": "4px 4px 0 #00C2FF",
        "comic-magenta": "4px 4px 0 #FF1F6D"
      },
      fontFamily: {
        bangers: ["Bangers", "Impact", "Arial Black", "sans-serif"],
        comic: ["'Comic Neue'", "'Comic Sans MS'", "cursive"],
        body: ["'Plus Jakarta Sans'", "system-ui", "sans-serif"],
        mono: ["'JetBrains Mono'", "ui-monospace", "monospace"]
      },
      keyframes: {
        wobble: { "0%,100%": { transform: "rotate(-3deg)" }, "50%": { transform: "rotate(3deg)" } },
        spinSlow: { to: { transform: "rotate(360deg)" } },
        shine: { "0%": { transform: "translateX(-120%) skewX(-20deg)" }, "100%": { transform: "translateX(220%) skewX(-20deg)" } },
        stripes: { to: { backgroundPosition: "28px 0" } }
      },
      animation: {
        wobble: "wobble 2.4s ease-in-out infinite",
        "spin-slow": "spinSlow 24s linear infinite",
        shine: "shine 2.8s ease-in-out infinite",
        stripes: "stripes 0.8s linear infinite"
      }
    }
  },
  plugins: []
};
