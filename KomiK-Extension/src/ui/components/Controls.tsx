import { AnimatePresence, motion } from "framer-motion";
import { useState, type ReactNode, type KeyboardEvent } from "react";
import { Icon } from "./Icon";
import { sfx } from "../sound";

export function Button({ children, tone = "white", size, icon, className = "", onClick, disabled, title, type = "button" }: {
  children?: ReactNode;
  tone?: "white" | "yellow" | "cyan" | "magenta" | "ink";
  size?: "sm";
  icon?: string;
  className?: string;
  onClick?: () => void;
  disabled?: boolean;
  title?: string;
  type?: "button" | "submit";
}) {
  const toneClass = tone === "white" ? "" : `btn-${tone}`;
  return (
    <button
      type={type}
      title={title}
      aria-label={title}
      disabled={disabled}
      className={`btn ${toneClass} ${size === "sm" ? "btn-sm" : ""} ${!children ? "btn-icon" : ""} ${className}`}
      onClick={() => {
        sfx.tick();
        onClick?.();
      }}
    >
      {icon && <Icon name={icon} size={size === "sm" ? 15 : 17} />}
      {children}
    </button>
  );
}

export function Sticker({ children, color = "#FFD700", className = "", rotate = 0, title }: { children: ReactNode; color?: string; className?: string; rotate?: number; title?: string }) {
  const dark = ["#08080A", "#FF1F6D"].includes(color);
  return (
    <span title={title} className={`sticker ${className}`} style={{ background: color, color: dark ? "#fff" : "#08080A", transform: rotate ? `rotate(${rotate}deg)` : undefined }}>
      {children}
    </span>
  );
}

export function Tabs<T extends string>({ tabs, value, onChange }: { tabs: Array<{ id: T; label: string; count?: number; disabled?: boolean; icon?: string }>; value: T; onChange: (id: T) => void }) {
  return (
    <div className="flex gap-1.5" role="tablist">
      {tabs.map((tab) => {
        const active = tab.id === value;
        return (
          <button
            key={tab.id}
            role="tab"
            aria-selected={active}
            disabled={tab.disabled}
            onClick={() => {
              sfx.tick();
              onChange(tab.id);
            }}
            className={`relative flex flex-1 items-center justify-center gap-1.5 rounded-t-md border-[2.5px] border-b-0 border-gutter px-2 pb-1.5 pt-2 font-bangers text-[15px] tracking-wider transition-colors disabled:opacity-40 ${
              active ? "bg-yellow text-gutter" : "bg-panel-card text-text hover:bg-cyan/30"
            }`}
          >
            {tab.icon && <Icon name={tab.icon} size={14} />}
            {tab.label}
            {tab.count !== undefined && tab.count > 0 && (
              <span className={`min-w-[20px] rounded-full border-2 border-gutter px-1 text-[12px] leading-[16px] ${active ? "bg-gutter text-yellow" : "bg-magenta text-white"}`}>{tab.count > 999 ? "999+" : tab.count}</span>
            )}
            {active && <motion.span layoutId="tab-underline" className="absolute inset-x-0 -bottom-[3px] h-[3px] bg-yellow" />}
          </button>
        );
      })}
    </div>
  );
}

export function Segmented<T extends string>({ options, value, onChange, className = "" }: { options: Array<{ id: T; label: string; hint?: string }>; value: T; onChange: (v: T) => void; className?: string }) {
  return (
    <div className={`flex overflow-hidden rounded-md border-[2.5px] border-gutter shadow-comic-xs ${className}`} role="radiogroup">
      {options.map((o, i) => (
        <button
          key={o.id}
          role="radio"
          aria-checked={o.id === value}
          title={o.hint}
          onClick={() => {
            sfx.tick();
            onChange(o.id);
          }}
          className={`relative flex-1 px-2.5 pb-1 pt-1.5 font-bangers text-[14px] tracking-wider transition-colors ${i > 0 ? "border-l-[2.5px] border-gutter" : ""} ${o.id === value ? "bg-cyan text-gutter" : "bg-panel text-text hover:bg-cyan/25"}`}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

export function Toggle({ checked, onChange, label, hint }: { checked: boolean; onChange: (v: boolean) => void; label: ReactNode; hint?: ReactNode }) {
  return (
    <label className="flex cursor-pointer items-start gap-3 py-1.5">
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={() => {
          sfx.tick();
          onChange(!checked);
        }}
        className={`relative mt-0.5 h-[26px] w-[48px] shrink-0 rounded-full border-[2.5px] border-gutter shadow-comic-xs transition-colors ${checked ? "bg-green" : "bg-panel-card"}`}
      >
        <motion.span
          layout
          transition={{ type: "spring", stiffness: 700, damping: 30 }}
          className={`absolute top-[1.5px] grid h-[18px] w-[18px] place-items-center rounded-full border-[2.5px] border-gutter ${checked ? "right-[2px] bg-yellow" : "left-[2px] bg-white"}`}
        />
      </button>
      <span className="min-w-0">
        <span className="block text-[13.5px] font-bold leading-tight">{label}</span>
        {hint && <span className="mt-0.5 block text-[12px] leading-snug text-muted">{hint}</span>}
      </span>
    </label>
  );
}

export function Field({ label, children, className = "" }: { label: string; children: ReactNode; className?: string }) {
  return (
    <label className={`block min-w-0 ${className}`}>
      <span className="label">{label}</span>
      {children}
    </label>
  );
}

const CHIP_COLORS = ["#FFD700", "#00C2FF", "#FF7A00", "#2FD17A", "#A78BFA", "#FF4D8B"];

export function chipColor(text: string): string {
  let h = 0;
  for (const ch of text.toLowerCase()) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
  return CHIP_COLORS[h % CHIP_COLORS.length];
}

/** Tag / people input: Enter or comma adds, Backspace removes the last, click × removes one. */
export function ChipInput({ values, onChange, placeholder, colorful = true }: { values: string[]; onChange: (v: string[]) => void; placeholder?: string; colorful?: boolean }) {
  const [draft, setDraft] = useState("");
  const add = (raw: string) => {
    const parts = raw.split(/[,;\n]/).map((p) => p.trim()).filter(Boolean);
    if (!parts.length) return;
    const lower = new Set(values.map((v) => v.toLowerCase()));
    const next = [...values];
    for (const p of parts) if (!lower.has(p.toLowerCase())) next.push(p);
    onChange(next);
    setDraft("");
    sfx.pop();
  };
  const onKey = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      add(draft);
    } else if (e.key === "Backspace" && !draft && values.length) {
      onChange(values.slice(0, -1));
    }
  };
  return (
    <div className="field flex min-h-[40px] flex-wrap items-center gap-1.5 !py-1.5" onClick={(e) => (e.currentTarget.querySelector("input") as HTMLInputElement)?.focus()}>
      <AnimatePresence initial={false}>
        {values.map((v) => (
          <motion.span
            key={v}
            layout
            initial={{ scale: 0.4, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.4, opacity: 0 }}
            transition={{ type: "spring", stiffness: 600, damping: 24 }}
            className="inline-flex items-center gap-1 rounded-full border-2 border-gutter py-[1px] pl-2 pr-1 text-[12px] font-bold text-gutter"
            style={{ background: colorful ? chipColor(v) : "#FFFDF6" }}
          >
            {v}
            <button
              type="button"
              aria-label={`Remove ${v}`}
              className="grid h-4 w-4 place-items-center rounded-full hover:bg-black/15"
              onClick={(e) => {
                e.stopPropagation();
                onChange(values.filter((x) => x !== v));
              }}
            >
              <Icon name="x" size={10} stroke={3.5} />
            </button>
          </motion.span>
        ))}
      </AnimatePresence>
      <input
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={onKey}
        onBlur={() => draft && add(draft)}
        onPaste={(e) => {
          const text = e.clipboardData.getData("text");
          if (/[,;\n]/.test(text)) {
            e.preventDefault();
            add(text);
          }
        }}
        placeholder={values.length ? "" : placeholder}
        className="min-w-[80px] flex-1 bg-transparent py-0.5 text-[13px] font-semibold outline-none placeholder:font-medium placeholder:text-muted/70"
      />
    </div>
  );
}

export function InkProgress({ value, moving = false, color = "#FFD700", label }: { value: number; moving?: boolean; color?: string; label?: string }) {
  const pct = Math.max(0, Math.min(100, value * 100));
  return (
    <div className="ink-track" role="progressbar" aria-valuenow={Math.round(pct)} aria-valuemin={0} aria-valuemax={100} aria-label={label}>
      <div className={`ink-fill ${moving ? "moving" : ""}`} style={{ width: `${pct}%`, backgroundColor: color, borderRightWidth: pct >= 99.5 || pct === 0 ? 0 : undefined }} />
    </div>
  );
}

export function Caption({ children, className = "", color }: { children: ReactNode; className?: string; color?: string }) {
  return (
    <span className={`caption ${className}`} style={color ? { background: color } : undefined}>
      {children}
    </span>
  );
}

export function Select<T extends string>({ value, onChange, options, className = "" }: { value: T; onChange: (v: T) => void; options: Array<{ id: T; label: string }>; className?: string }) {
  return (
    <select value={value} onChange={(e) => onChange(e.target.value as T)} className={`field cursor-pointer appearance-none bg-[length:12px] bg-[right_10px_center] bg-no-repeat pr-8 ${className}`} style={{ backgroundImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2308080A' stroke-width='4'%3E%3Cpath d='M5 9l7 7 7-7'/%3E%3C/svg%3E\")" }}>
      {options.map((o) => (
        <option key={o.id} value={o.id}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
