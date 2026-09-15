"use client";

import { useEffect, useState } from "react";
import { Moon, Sun } from "lucide-react";

type Theme = "light" | "dark";

function readTheme(): Theme {
  if (typeof document === "undefined") return "dark";
  return document.documentElement.classList.contains("light") ? "light" : "dark";
}

/** Day / night switch. The choice is remembered; until then the site follows the system theme. */
export default function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>("dark");

  useEffect(() => {
    setTheme(readTheme());

    const media = window.matchMedia("(prefers-color-scheme: light)");
    const onSystemChange = (e: MediaQueryListEvent) => {
      try {
        if (localStorage.getItem("komik-theme")) return;
      } catch {
        /* storage blocked: follow the system */
      }
      apply(e.matches ? "light" : "dark", false);
    };
    media.addEventListener("change", onSystemChange);
    return () => media.removeEventListener("change", onSystemChange);
  }, []);

  function apply(next: Theme, remember: boolean) {
    const root = document.documentElement;
    root.classList.add("theme-transition");
    root.classList.toggle("light", next === "light");
    root.classList.toggle("dark", next === "dark");
    window.setTimeout(() => root.classList.remove("theme-transition"), 400);
    if (remember) {
      try {
        localStorage.setItem("komik-theme", next);
      } catch {
        /* ignore */
      }
    }
    setTheme(next);
  }

  const isLight = theme === "light";

  return (
    <button
      type="button"
      onClick={() => apply(isLight ? "dark" : "light", true)}
      className="relative inline-flex h-10 w-10 items-center justify-center overflow-hidden border-2 border-black bg-amber text-black shadow-[2px_2px_0_#000] transition-transform hover:-translate-x-0.5 hover:-translate-y-0.5 hover:shadow-[4px_4px_0_#000] active:translate-x-0 active:translate-y-0 active:shadow-[1px_1px_0_#000]"
      aria-label={isLight ? "Switch to dark theme" : "Switch to light theme"}
      title={isLight ? "Dark theme" : "Light theme"}
    >
      <Sun className="theme-toggle-icon theme-toggle-sun absolute h-5 w-5 stroke-[2.5]" />
      <Moon className="theme-toggle-icon theme-toggle-moon absolute h-5 w-5 stroke-[2.5]" />
    </button>
  );
}
