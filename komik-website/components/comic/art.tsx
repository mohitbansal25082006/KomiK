"use client";

import React from "react";
import { CYAN, INK, MAGENTA, YELLOW, rng, usePage } from "./kit";

/* ------------------------------------------------------------------ */
/*  Illustration library for "Cyberpunk Chronicles #01"                */
/* ------------------------------------------------------------------ */

type P = [number, number];

export type Pose = {
  head: P;
  neck: P;
  hip: P;
  lElbow: P;
  lHand: P;
  rElbow: P;
  rHand: P;
  lKnee: P;
  lFoot: P;
  rKnee: P;
  rFoot: P;
};

const LEGS_STAND = { lKnee: [-7, -44] as P, lFoot: [-12, 0] as P, rKnee: [7, -44] as P, rFoot: [13, 0] as P };

export const POSES: Record<string, Pose> = {
  stand: { head: [0, -166], neck: [0, -146], hip: [0, -88], lElbow: [-10, -116], lHand: [-8, -86], rElbow: [10, -116], rHand: [11, -86], ...LEGS_STAND },
  heroic: { head: [0, -168], neck: [0, -148], hip: [0, -88], lElbow: [-30, -120], lHand: [-12, -94], rElbow: [30, -120], rHand: [12, -94], lKnee: [-16, -44], lFoot: [-28, 0], rKnee: [16, -44], rFoot: [28, 0] },
  run: { head: [16, -160], neck: [11, -142], hip: [-2, -86], lElbow: [-22, -118], lHand: [-34, -98], rElbow: [30, -118], rHand: [44, -138], lKnee: [-18, -46], lFoot: [-46, -30], rKnee: [30, -58], rFoot: [28, -12] },
  leap: { head: [62, -122], neck: [44, -116], hip: [-8, -100], lElbow: [20, -94], lHand: [2, -80], rElbow: [74, -130], rHand: [100, -142], lKnee: [-42, -106], lFoot: [-80, -96], rKnee: [22, -70], rFoot: [-4, -52] },
  punch: { head: [12, -162], neck: [7, -144], hip: [-4, -88], lElbow: [-8, -120], lHand: [8, -130], rElbow: [42, -140], rHand: [78, -142], lKnee: [-18, -44], lFoot: [-42, 0], rKnee: [26, -48], rFoot: [42, 0] },
  reach: { head: [10, -160], neck: [5, -142], hip: [-4, -86], lElbow: [-8, -114], lHand: [-6, -86], rElbow: [28, -118], rHand: [54, -106], ...LEGS_STAND },
  glide: { head: [32, -150], neck: [20, -134], hip: [-20, -92], lElbow: [-18, -160], lHand: [-50, -182], rElbow: [52, -152], rHand: [84, -168], lKnee: [-40, -52], lFoot: [-62, -22], rKnee: [-18, -48], rFoot: [-30, -10] },
  stagger: { head: [-26, -158], neck: [-18, -140], hip: [0, -88], lElbow: [-42, -126], lHand: [-56, -150], rElbow: [10, -148], rHand: [-18, -166], lKnee: [-14, -44], lFoot: [-32, 0], rKnee: [16, -46], rFoot: [26, 0] },
  kneel: { head: [4, -112], neck: [2, -94], hip: [-6, -48], lElbow: [-12, -66], lHand: [-8, -40], rElbow: [16, -68], rHand: [20, -42], lKnee: [-8, -2], lFoot: [-44, 0], rKnee: [26, -40], rFoot: [26, 0] },
  point: { head: [0, -166], neck: [0, -146], hip: [0, -88], lElbow: [-12, -116], lHand: [-10, -86], rElbow: [36, -138], rHand: [70, -142], lKnee: [-10, -44], lFoot: [-18, 0], rKnee: [12, -44], rFoot: [22, 0] },
  give: { head: [16, -150], neck: [9, -132], hip: [0, -80], lElbow: [-8, -104], lHand: [-4, -78], rElbow: [28, -104], rHand: [52, -112], ...LEGS_STAND },
  shout: { head: [4, -160], neck: [2, -142], hip: [0, -86], lElbow: [-24, -150], lHand: [-36, -178], rElbow: [26, -150], rHand: [38, -178], ...LEGS_STAND },
  slide: { head: [44, -62], neck: [28, -56], hip: [-20, -30], lElbow: [-6, -72], lHand: [-22, -94], rElbow: [44, -30], rHand: [60, -10], lKnee: [-10, -6], lFoot: [-46, -4], rKnee: [30, -14], rFoot: [74, -4] },
  flip: { head: [0, -34], neck: [0, -52], hip: [0, -110], lElbow: [-22, -40], lHand: [-36, -22], rElbow: [22, -40], rHand: [38, -22], lKnee: [-26, -128], lFoot: [-10, -150], rKnee: [30, -132], rFoot: [14, -152] },
  hit: { head: [-30, -150], neck: [-22, -134], hip: [4, -84], lElbow: [-44, -110], lHand: [-60, -96], rElbow: [8, -120], rHand: [26, -108], lKnee: [-6, -40], lFoot: [-30, 0], rKnee: [26, -42], rFoot: [40, 0] },
};

type FigureStyle = {
  skin: string;
  torso: string;
  arms: string;
  legs: string;
  boots: string;
  hands?: string;
  scale?: number;
};

function add(a: P, b: P): P {
  return [a[0] + b[0], a[1] + b[1]];
}

function Limb({ pts, color, width }: { pts: P[]; color: string; width: number }) {
  const d = `M${pts.map((p) => p.join(",")).join(" L")}`;
  return (
    <g fill="none" strokeLinecap="round" strokeLinejoin="round">
      <path d={d} stroke={INK} strokeWidth={width + 6} />
      <path d={d} stroke={color} strokeWidth={width} />
    </g>
  );
}

function Figure({
  pose,
  style,
  head,
  back,
  torsoExtra,
}: {
  pose: Pose;
  style: FigureStyle;
  head: React.ReactNode;
  back?: React.ReactNode;
  torsoExtra?: React.ReactNode;
}) {
  const { neck, hip } = pose;
  const vx = hip[0] - neck[0];
  const vy = hip[1] - neck[1];
  const len = Math.hypot(vx, vy) || 1;
  const n: P = [-vy / len, vx / len];
  const shL = add(neck, [n[0] * 14, n[1] * 14]);
  const shR = add(neck, [-n[0] * 14, -n[1] * 14]);
  const hpL = add(hip, [n[0] * 10, n[1] * 10]);
  const hpR = add(hip, [-n[0] * 10, -n[1] * 10]);
  const s = style;
  return (
    <g>
      {back}
      {/* back limbs */}
      <Limb pts={[hpL, pose.lKnee, pose.lFoot]} color={s.legs} width={13} />
      <circle cx={pose.lFoot[0]} cy={pose.lFoot[1]} r={8} fill={s.boots} stroke={INK} strokeWidth={3} />
      <Limb pts={[shL, pose.lElbow, pose.lHand]} color={s.arms} width={11} />
      <circle cx={pose.lHand[0]} cy={pose.lHand[1]} r={6.5} fill={s.hands ?? s.skin} stroke={INK} strokeWidth={3} />
      {/* torso */}
      <path
        d={`M${shL.join(",")} L${shR.join(",")} L${hpR.join(",")} L${hpL.join(",")} Z`}
        fill={s.torso}
        stroke={INK}
        strokeWidth={4}
        strokeLinejoin="round"
      />
      {torsoExtra}
      {/* front limbs */}
      <Limb pts={[hpR, pose.rKnee, pose.rFoot]} color={s.legs} width={13} />
      <circle cx={pose.rFoot[0]} cy={pose.rFoot[1]} r={8} fill={s.boots} stroke={INK} strokeWidth={3} />
      <Limb pts={[shR, pose.rElbow, pose.rHand]} color={s.arms} width={11} />
      <circle cx={pose.rHand[0]} cy={pose.rHand[1]} r={6.5} fill={s.hands ?? s.skin} stroke={INK} strokeWidth={3} />
      {head}
    </g>
  );
}

type Placement = { x: number; y: number; s?: number; flip?: boolean };

function place({ x, y, s = 1, flip }: Placement) {
  return `translate(${x} ${y}) scale(${flip ? -s : s} ${s})`;
}

/* ------------------------------ VEX ------------------------------- */

export function Vex({ pose = "stand", back = false, scarf = true, ...pl }: Placement & { pose?: keyof typeof POSES; back?: boolean; scarf?: boolean }) {
  const p = POSES[pose];
  const [hx, hy] = p.head;
  const [nx, ny] = p.neck;
  const scarfPath = `M${nx - 4},${ny + 2} C${nx - 30},${ny - 6} ${nx - 46},${ny + 22} ${nx - 78},${ny + 8} C${nx - 60},${ny + 26} ${nx - 34},${ny + 22} ${nx + 6},${ny + 10} Z`;
  const head = (
    <g>
      {/* hair back */}
      <path
        d={`M${hx - 20},${hy + 6} L${hx - 34},${hy - 4} L${hx - 22},${hy - 12} L${hx - 32},${hy - 24} L${hx - 14},${hy - 22} L${hx - 12},${hy - 34} L${hx + 2},${hy - 22} L${hx + 14},${hy - 30} L${hx + 14},${hy - 16} L${hx + 22},${hy - 10} L${hx + 6},${hy - 4} Z`}
        fill="#1B1030"
        stroke={INK}
        strokeWidth={3}
        strokeLinejoin="round"
      />
      <circle cx={hx} cy={hy} r={16} fill={back ? "#1B1030" : "#D9A066"} stroke={INK} strokeWidth={4} />
      {!back && (
        <>
          <path d={`M${hx - 14},${hy - 6} Q${hx - 2},${hy - 22} ${hx + 16},${hy - 8} L${hx + 4},${hy - 12} Z`} fill="#1B1030" />
          <path d={`M${hx - 8},${hy - 16} L${hx - 2},${hy - 8}`} stroke={MAGENTA} strokeWidth={4} strokeLinecap="round" />
          <rect x={hx - 2} y={hy - 5} width={21} height={8} rx={3} fill={CYAN} stroke={INK} strokeWidth={2.5} />
          <line x1={hx + 2} y1={hy - 3} x2={hx + 14} y2={hy - 3} stroke="#fff" strokeWidth={1.5} />
          <path d={`M${hx + 6},${hy + 9} L${hx + 13},${hy + 8}`} stroke={INK} strokeWidth={2} strokeLinecap="round" />
        </>
      )}
      {back && <path d={`M${hx - 10},${hy - 14} L${hx - 4},${hy - 4}`} stroke={MAGENTA} strokeWidth={4} strokeLinecap="round" />}
    </g>
  );
  return (
    <g transform={place(pl)}>
      <Figure
        pose={p}
        style={{ skin: "#D9A066", torso: YELLOW, arms: YELLOW, legs: "#1C1B2B", boots: "#0A0A0F", hands: "#1C1B2B" }}
        back={scarf ? <path d={scarfPath} fill={MAGENTA} stroke={INK} strokeWidth={3} strokeLinejoin="round" /> : null}
        torsoExtra={
          <g>
            <line x1={nx} y1={ny + 4} x2={(nx + p.hip[0]) / 2} y2={(ny + p.hip[1]) / 2 + 10} stroke={INK} strokeWidth={2.5} />
            <path d={`M${nx - 10},${ny + 2} L${nx + 10},${ny + 2} L${nx + 4},${ny + 12} Z`} fill={MAGENTA} stroke={INK} strokeWidth={2.5} />
          </g>
        }
        head={head}
      />
    </g>
  );
}

export function VexFace({ x, y, s = 1, flip, expr = "neutral" }: Placement & { expr?: "neutral" | "smirk" | "shout" | "worried" }) {
  const { url } = usePage();
  return (
    <g transform={place({ x, y, s, flip })}>
      {/* shoulders */}
      <path d="M-120,150 Q-100,70 -30,62 L30,62 Q100,70 120,150 Z" fill={YELLOW} stroke={INK} strokeWidth={6} />
      <path d="M-120,150 Q-100,70 -30,62 L30,62 Q100,70 120,150 Z" fill={url("dots")} />
      <path d="M-44,58 Q0,96 44,58 L56,84 Q0,120 -56,84 Z" fill={MAGENTA} stroke={INK} strokeWidth={5} strokeLinejoin="round" />
      {/* neck + head */}
      <rect x={-16} y={36} width={32} height={30} fill="#C08850" stroke={INK} strokeWidth={5} />
      <path d="M-58,-20 Q-60,-86 0,-90 Q60,-86 58,-20 Q56,34 0,56 Q-56,34 -58,-20 Z" fill="#D9A066" stroke={INK} strokeWidth={6} />
      <path d="M-58,-20 Q-50,30 -10,52 Q-40,20 -44,-18 Z" fill="#B97F4A" opacity={0.7} />
      {/* hair */}
      <path
        d="M-72,6 L-92,-30 L-66,-38 L-90,-72 L-56,-72 L-62,-110 L-28,-86 L-10,-124 L10,-92 L40,-118 L42,-84 L78,-96 L62,-60 L90,-50 L60,-30 L44,-58 L10,-50 L-20,-62 L-48,-40 L-54,4 Z"
        fill="#1B1030"
        stroke={INK}
        strokeWidth={5}
        strokeLinejoin="round"
      />
      <path d="M-28,-86 L-6,-54" stroke={MAGENTA} strokeWidth={10} strokeLinecap="round" />
      {/* visor */}
      <path d="M-66,-28 L66,-28 L60,2 L-60,2 Z" fill={CYAN} stroke={INK} strokeWidth={6} strokeLinejoin="round" />
      <path d="M-50,-20 L20,-20" stroke="#fff" strokeWidth={5} strokeLinecap="round" opacity={0.9} />
      <path d="M30,-20 L42,-20" stroke="#fff" strokeWidth={5} strokeLinecap="round" opacity={0.9} />
      {/* mouth */}
      {expr === "neutral" && <path d="M-16,30 L16,30" stroke={INK} strokeWidth={5} strokeLinecap="round" />}
      {expr === "smirk" && <path d="M-18,30 Q6,34 22,20" stroke={INK} strokeWidth={5} strokeLinecap="round" fill="none" />}
      {expr === "worried" && <path d="M-16,34 Q0,24 16,34" stroke={INK} strokeWidth={5} strokeLinecap="round" fill="none" />}
      {expr === "shout" && <path d="M-22,20 Q0,14 22,20 Q18,50 0,52 Q-18,50 -22,20 Z" fill="#4A0F1E" stroke={INK} strokeWidth={5} />}
    </g>
  );
}

/* ----------------------------- BYTE ------------------------------- */

export function Byte({ x, y, s = 1, flip, mood = "normal" }: Placement & { mood?: "normal" | "alarm" | "happy" | "wink" | "plane" }) {
  const eye = mood === "alarm" ? "#FF3B3B" : CYAN;
  return (
    <g transform={place({ x, y, s, flip })}>
      <line x1={0} y1={-30} x2={8} y2={-48} stroke={INK} strokeWidth={4} strokeLinecap="round" />
      <circle cx={8} cy={-50} r={6} fill={mood === "alarm" ? "#FF3B3B" : MAGENTA} stroke={INK} strokeWidth={3} />
      <ellipse cx={-30} cy={-6} rx={12} ry={5} fill="#3A3F52" stroke={INK} strokeWidth={3} />
      <ellipse cx={30} cy={-6} rx={12} ry={5} fill="#3A3F52" stroke={INK} strokeWidth={3} />
      <circle cx={0} cy={0} r={30} fill="#E8ECF5" stroke={INK} strokeWidth={5} />
      <path d="M-26,12 Q0,34 26,12 Q10,30 -26,12 Z" fill="#AEB6C8" />
      <circle cx={0} cy={-2} r={16} fill={INK} />
      {mood === "happy" && <path d="M-9,0 Q0,-12 9,0" stroke={eye} strokeWidth={5} fill="none" strokeLinecap="round" />}
      {mood === "wink" && (
        <>
          <path d="M-9,-2 L9,-2" stroke={eye} strokeWidth={5} strokeLinecap="round" />
          <circle cx={18} cy={18} r={4} fill={MAGENTA} opacity={0.7} />
        </>
      )}
      {mood === "plane" && <path d="M-10,-2 L10,-2 M0,-10 L3,-2 L0,8 M-6,6 L6,6" stroke={eye} strokeWidth={3} strokeLinecap="round" />}
      {(mood === "normal" || mood === "alarm") && (
        <>
          <circle cx={0} cy={-2} r={9} fill={eye} />
          <circle cx={3} cy={-5} r={3} fill="#fff" />
        </>
      )}
    </g>
  );
}

export function EnemyDrone({ x, y, s = 1, flip }: Placement) {
  return (
    <g transform={place({ x, y, s, flip })}>
      <path d="M-44,0 Q0,-30 44,0 Q0,18 -44,0 Z" fill="#5B6478" stroke={INK} strokeWidth={4} />
      <rect x={-50} y={-4} width={100} height={8} rx={4} fill="#2B303D" stroke={INK} strokeWidth={3} />
      <circle cx={0} cy={4} r={10} fill="#FF3B3B" stroke={INK} strokeWidth={3} />
      <circle cx={-3} cy={1} r={3} fill="#fff" />
      <path d="M-30,-14 L-40,-26 M30,-14 L40,-26" stroke={INK} strokeWidth={3} />
      <ellipse cx={-40} cy={-27} rx={14} ry={3} fill="#AEB6C8" stroke={INK} strokeWidth={2} />
      <ellipse cx={40} cy={-27} rx={14} ry={3} fill="#AEB6C8" stroke={INK} strokeWidth={2} />
    </g>
  );
}

/* ---------------------------- STRATUS ----------------------------- */

export function CloudHelmet({ hx, hy, r = 22, cracked = false }: { hx: number; hy: number; r?: number; cracked?: boolean }) {
  const { url } = usePage();
  return (
    <g>
      <g fill={url("steel")} stroke={INK} strokeWidth={4}>
        <circle cx={hx - r * 0.7} cy={hy + r * 0.1} r={r * 0.62} />
        <circle cx={hx + r * 0.72} cy={hy + r * 0.12} r={r * 0.6} />
        <circle cx={hx - r * 0.25} cy={hy - r * 0.55} r={r * 0.7} />
        <circle cx={hx + r * 0.4} cy={hy - r * 0.4} r={r * 0.6} />
      </g>
      <circle cx={hx} cy={hy} r={r * 0.9} fill={url("steel")} />
      <path d={`M${hx - r * 1.05},${hy + r * 0.1} Q${hx},${hy - r * 0.1} ${hx + r * 1.05},${hy + r * 0.1}`} fill="none" stroke={INK} strokeWidth={0} />
      <rect x={hx - r * 0.35} y={hy - r * 0.12} width={r * 1.2} height={r * 0.32} rx={r * 0.12} fill="#FF2A2A" stroke={INK} strokeWidth={3} />
      {cracked && <path d={`M${hx - r * 0.3},${hy - r} L${hx - r * 0.05},${hy - r * 0.45} L${hx - r * 0.35},${hy - r * 0.15} L${hx},${hy + r * 0.3}`} stroke={INK} strokeWidth={3} fill="none" />}
    </g>
  );
}

export function Stratus({ pose = "stand", masked = true, cape = true, ...pl }: Placement & { pose?: keyof typeof POSES; masked?: boolean; cape?: boolean }) {
  const p = POSES[pose];
  const [hx, hy] = p.head;
  const [nx, ny] = p.neck;
  const head = masked ? (
    <CloudHelmet hx={hx} hy={hy} />
  ) : (
    <g>
      <circle cx={hx} cy={hy} r={16} fill="#E2B38A" stroke={INK} strokeWidth={4} />
      <path d={`M${hx - 16},${hy - 2} Q${hx - 14},${hy - 22} ${hx + 2},${hy - 20} Q${hx + 18},${hy - 20} ${hx + 16},${hy - 4} L${hx + 6},${hy - 12} L${hx - 6},${hy - 8} Z`} fill="#4B3B2F" stroke={INK} strokeWidth={2.5} />
      <circle cx={hx + 7} cy={hy - 2} r={2.2} fill={INK} />
    </g>
  );
  return (
    <g transform={place({ ...pl, s: (pl.s ?? 1) * 1.12 })}>
      <Figure
        pose={p}
        style={{ skin: "#E2B38A", torso: "#DDE3EE", arms: "#B8C0D0", legs: "#4A5264", boots: "#1E2230", hands: "#7B8494" }}
        back={
          cape ? (
            <path
              d={`M${nx - 12},${ny} L${nx + 12},${ny} L${p.hip[0] - 24},${p.hip[1] + 70} L${p.hip[0] - 70},${p.hip[1] + 60} Z`}
              fill="#3A4150"
              stroke={INK}
              strokeWidth={4}
              strokeLinejoin="round"
            />
          ) : null
        }
        torsoExtra={
          <g>
            <circle cx={(nx + p.hip[0]) / 2} cy={(ny + p.hip[1]) / 2 - 6} r={8} fill="#7FD4FF" stroke={INK} strokeWidth={3} />
            <path d={`M${(nx + p.hip[0]) / 2 - 6},${(ny + p.hip[1]) / 2 - 5} q3,-6 6,-2 q3,-5 6,1`} stroke="#fff" strokeWidth={2} fill="none" />
          </g>
        }
        head={head}
      />
    </g>
  );
}

export function StratusHelmetFace({ x, y, s = 1, flip }: Placement) {
  const { url } = usePage();
  return (
    <g transform={place({ x, y, s, flip })}>
      <path d="M-130,160 Q-110,70 -40,60 L40,60 Q110,70 130,160 Z" fill="#DDE3EE" stroke={INK} strokeWidth={6} />
      <path d="M-130,160 Q-110,70 -40,60 L40,60 Q110,70 130,160 Z" fill={url("dots")} />
      <circle cx={0} cy={104} r={18} fill="#7FD4FF" stroke={INK} strokeWidth={5} />
      <g fill={url("steel")} stroke={INK} strokeWidth={6}>
        <circle cx={-62} cy={0} r={48} />
        <circle cx={64} cy={4} r={46} />
        <circle cx={-22} cy={-56} r={54} />
        <circle cx={38} cy={-44} r={48} />
        <circle cx={0} cy={10} r={62} />
      </g>
      <circle cx={0} cy={4} r={58} fill={url("steel")} />
      <path d="M-70,-4 L70,-4 L62,26 L-62,26 Z" fill="#FF2A2A" stroke={INK} strokeWidth={6} strokeLinejoin="round" />
      <path d="M-50,4 L30,4" stroke="#FFB3B3" strokeWidth={5} strokeLinecap="round" />
      <circle cx={0} cy={11} r={40} fill={url("glowR")} opacity={0.5} />
    </g>
  );
}

export function StratusFace({ x, y, s = 1, flip, expr = "sad" }: Placement & { expr?: "sad" | "hopeful" }) {
  const { url } = usePage();
  return (
    <g transform={place({ x, y, s, flip })}>
      <path d="M-130,160 Q-110,70 -40,60 L40,60 Q110,70 130,160 Z" fill="#DDE3EE" stroke={INK} strokeWidth={6} />
      <rect x={-18} y={34} width={36} height={32} fill="#C9966E" stroke={INK} strokeWidth={5} />
      <path d="M-56,-20 Q-58,-84 0,-88 Q58,-84 56,-20 Q54,34 0,56 Q-54,34 -56,-20 Z" fill="#E2B38A" stroke={INK} strokeWidth={6} />
      <path d="M-50,10 Q-30,48 0,54 Q30,48 50,10 Q30,34 0,38 Q-30,34 -50,10 Z" fill={url("dots")} />
      <path d="M-62,-26 Q-70,-100 0,-102 Q72,-100 62,-26 L50,-58 L24,-44 L0,-66 L-26,-46 L-50,-60 Z" fill="#4B3B2F" stroke={INK} strokeWidth={5} strokeLinejoin="round" />
      {/* eyes */}
      <path d="M-36,-18 L-12,-14 M12,-14 L36,-18" stroke={INK} strokeWidth={5} strokeLinecap="round" />
      <circle cx={-24} cy={-4} r={5} fill={INK} />
      <circle cx={24} cy={-4} r={5} fill={INK} />
      {expr === "sad" && <path d="M-16,30 Q0,22 16,30" stroke={INK} strokeWidth={5} fill="none" strokeLinecap="round" />}
      {expr === "hopeful" && <path d="M-16,26 Q0,34 16,26" stroke={INK} strokeWidth={5} fill="none" strokeLinecap="round" />}
      <path d="M-40,6 L-38,14" stroke="#6FB7FF" strokeWidth={4} strokeLinecap="round" />
    </g>
  );
}

/* ----------------------------- ORRIN ------------------------------ */

export function Orrin({ pose = "stand", ...pl }: Placement & { pose?: keyof typeof POSES }) {
  const p = POSES[pose];
  const [hx, hy] = p.head;
  const head = (
    <g>
      <circle cx={hx} cy={hy} r={16} fill="#E0AC69" stroke={INK} strokeWidth={4} />
      <path d={`M${hx - 16},${hy - 2} Q${hx - 22},${hy - 10} ${hx - 14},${hy - 14}`} stroke="#F2F2F2" strokeWidth={6} fill="none" strokeLinecap="round" />
      <path d={`M${hx - 8},${hy + 6} Q${hx + 4},${hy + 34} ${hx + 18},${hy + 6} Z`} fill="#F2F2F2" stroke={INK} strokeWidth={3} strokeLinejoin="round" />
      <circle cx={hx + 8} cy={hy - 3} r={5} fill="#BFE9FF" stroke={INK} strokeWidth={2.5} />
    </g>
  );
  return (
    <g transform={place(pl)}>
      <Figure pose={p} style={{ skin: "#E0AC69", torso: "#8B5A2B", arms: "#8B5A2B", legs: "#3B2F2F", boots: "#1A1410" }} head={head} />
    </g>
  );
}

export function OrrinFace({ x, y, s = 1, flip, expr = "grave" }: Placement & { expr?: "grave" | "smile" | "shout" }) {
  const { url } = usePage();
  return (
    <g transform={place({ x, y, s, flip })}>
      <path d="M-130,160 Q-110,70 -40,60 L40,60 Q110,70 130,160 Z" fill="#8B5A2B" stroke={INK} strokeWidth={6} />
      <path d="M-130,160 Q-110,70 -40,60 L40,60 Q110,70 130,160 Z" fill={url("dots")} />
      <path d="M-56,-20 Q-58,-84 0,-88 Q58,-84 56,-20 Q54,34 0,56 Q-54,34 -56,-20 Z" fill="#E0AC69" stroke={INK} strokeWidth={6} />
      <path d="M-60,-6 Q-80,-30 -58,-50 M60,-6 Q80,-30 58,-50" stroke="#F2F2F2" strokeWidth={16} strokeLinecap="round" fill="none" />
      <path d="M-44,10 Q-40,110 0,120 Q40,110 44,10 Q20,30 0,26 Q-20,30 -44,10 Z" fill="#F4F4F4" stroke={INK} strokeWidth={5} strokeLinejoin="round" />
      {expr === "shout" && <ellipse cx={0} cy={36} rx={14} ry={12} fill="#4A0F1E" stroke={INK} strokeWidth={4} />}
      {expr === "smile" && <path d="M-14,32 Q0,42 14,32" stroke={INK} strokeWidth={4} fill="none" strokeLinecap="round" />}
      <path d="M-40,-30 L-10,-26 M10,-26 L40,-30" stroke="#F2F2F2" strokeWidth={8} strokeLinecap="round" />
      <g fill="#BFE9FF" stroke={INK} strokeWidth={5}>
        <circle cx={-24} cy={-8} r={17} />
        <circle cx={24} cy={-8} r={17} />
      </g>
      <line x1={-7} y1={-8} x2={7} y2={-8} stroke={INK} strokeWidth={5} />
      <circle cx={-24} cy={-8} r={4} fill={INK} />
      <circle cx={24} cy={-8} r={4} fill={INK} />
      {expr === "smile" && <rect x={20} y={-70} width={30} height={10} rx={3} fill="#fff" stroke={INK} strokeWidth={3} transform="rotate(20 35 -65)" />}
    </g>
  );
}

/* --------------------------- Civilians ---------------------------- */

export function Citizen({ x, y, s = 1, flip, color = "#2B2240", screen = true, seed = 1 }: Placement & { color?: string; screen?: boolean; seed?: number }) {
  const r = rng(seed);
  const hair = ["#1B1030", "#6B3F1F", "#C94F7C", "#2E4A7A"][Math.floor(r() * 4)];
  return (
    <g transform={place({ x, y, s, flip })}>
      <path d="M-34,0 Q-34,-70 0,-74 Q34,-70 34,0 Z" fill={color} stroke={INK} strokeWidth={4} />
      <circle cx={0} cy={-94} r={22} fill="#C99A6E" stroke={INK} strokeWidth={4} />
      <path d="M-22,-96 Q-22,-122 0,-120 Q24,-122 22,-96 Q10,-108 -22,-96 Z" fill={hair} stroke={INK} strokeWidth={3} />
      {screen && (
        <>
          <rect x={12} y={-66} width={20} height={32} rx={3} fill={CYAN} stroke={INK} strokeWidth={3} transform="rotate(-15 22 -50)" />
          <circle cx={4} cy={-90} r={26} fill={CYAN} opacity={0.18} />
        </>
      )}
    </g>
  );
}

/* ---------------------------- Scenery ----------------------------- */

export function Sky({ w, h, kind = "skyNight" }: { w: number; h: number; kind?: "skyNight" | "skyNeon" | "skyDawn" }) {
  const { url } = usePage();
  return <rect width={w} height={h} fill={url(kind)} />;
}

export function Stars({ w, h, count = 40, seed = 2 }: { w: number; h: number; count?: number; seed?: number }) {
  const r = rng(seed);
  return (
    <g fill="#fff">
      {Array.from({ length: count }).map((_, i) => (
        <circle key={i} cx={r() * w} cy={r() * h} r={0.6 + r() * 1.4} opacity={0.3 + r() * 0.6} />
      ))}
    </g>
  );
}

export function Moon({ cx, cy, r }: { cx: number; cy: number; r: number }) {
  const { url } = usePage();
  return (
    <g>
      <circle cx={cx} cy={cy} r={r * 1.6} fill={url("glowY")} opacity={0.35} />
      <circle cx={cx} cy={cy} r={r} fill={YELLOW} stroke={INK} strokeWidth={5} />
      <circle cx={cx} cy={cy} r={r} fill={url("dotsBig")} />
      <circle cx={cx - r * 0.3} cy={cy - r * 0.2} r={r * 0.16} fill="#E6B800" />
      <circle cx={cx + r * 0.35} cy={cy + r * 0.3} r={r * 0.1} fill="#E6B800" />
    </g>
  );
}

export function Skyline({
  w,
  baseY,
  seed = 1,
  minH = 60,
  maxH = 220,
  color = "#120F2A",
  windows = true,
  minW = 30,
  maxW = 80,
  outline = true,
}: {
  w: number;
  baseY: number;
  seed?: number;
  minH?: number;
  maxH?: number;
  color?: string;
  windows?: boolean;
  minW?: number;
  maxW?: number;
  outline?: boolean;
}) {
  const r = rng(seed);
  const buildings: React.ReactNode[] = [];
  let x = -10;
  let i = 0;
  const winColors = [YELLOW, CYAN, MAGENTA, "#FFF1B8"];
  while (x < w + 10) {
    const bw = minW + r() * (maxW - minW);
    const bh = minH + r() * (maxH - minH);
    const top = baseY - bh;
    const antenna = r() > 0.7;
    const wins: React.ReactNode[] = [];
    if (windows) {
      for (let wy = top + 12; wy < baseY - 10; wy += 16) {
        for (let wx = x + 8; wx < x + bw - 10; wx += 13) {
          if (r() > 0.62) {
            wins.push(<rect key={`${wx}-${wy}`} x={wx} y={wy} width={5} height={8} fill={winColors[Math.floor(r() * 4)]} opacity={0.9} />);
          }
        }
      }
    }
    buildings.push(
      <g key={i++}>
        {antenna && <line x1={x + bw / 2} y1={top} x2={x + bw / 2} y2={top - 24} stroke={INK} strokeWidth={3} />}
        {antenna && <circle cx={x + bw / 2} cy={top - 25} r={3} fill="#FF3B3B" />}
        <rect x={x} y={top} width={bw} height={bh + 400} fill={color} stroke={outline ? INK : "none"} strokeWidth={3} />
        {wins}
      </g>,
    );
    x += bw + (r() > 0.6 ? 4 : 0);
  }
  return <g>{buildings}</g>;
}

export function NeonSign({ x, y, text, color = MAGENTA, size = 26, rotate = 0, vertical = false }: { x: number; y: number; text: string; color?: string; size?: number; rotate?: number; vertical?: boolean }) {
  const chars = vertical ? text.split("") : [text];
  const bw = vertical ? size * 1.5 : text.length * size * 0.62 + size;
  const bh = vertical ? chars.length * size * 1.05 + size * 0.7 : size * 1.6;
  return (
    <g transform={`rotate(${rotate} ${x} ${y})`}>
      <rect x={x - bw / 2} y={y - bh / 2} width={bw} height={bh} fill="#0A0616" stroke={color} strokeWidth={4} rx={6} />
      <rect x={x - bw / 2 - 6} y={y - bh / 2 - 6} width={bw + 12} height={bh + 12} fill="none" stroke={color} strokeWidth={10} opacity={0.18} rx={10} />
      <text
        x={x}
        y={vertical ? y - bh / 2 + size * 1.1 : y + size * 0.36}
        textAnchor="middle"
        fontSize={size}
        fill="#fff"
        stroke={color}
        strokeWidth={vertical ? 1 : 2}
        paintOrder="stroke"
        style={{ fontFamily: "var(--font-bangers), Impact, sans-serif", letterSpacing: 2 }}
      >
        {chars.map((c, i) => (
          <tspan key={i} x={x} dy={i === 0 ? 0 : size * 1.05}>
            {c}
          </tspan>
        ))}
      </text>
    </g>
  );
}

export function Blimp({ x, y, s = 1, screen = "UPLOAD EVERYTHING.\nOWN NOTHING.", glitch = false }: { x: number; y: number; s?: number; screen?: string; glitch?: boolean }) {
  const lines = screen.split("\n");
  return (
    <g transform={`translate(${x} ${y}) scale(${s})`}>
      <polygon points="-40,40 -140,260 60,260" fill={CYAN} opacity={0.12} />
      <g fill="#DDE3EE" stroke={INK} strokeWidth={5}>
        <circle cx={-110} cy={10} r={42} />
        <circle cx={110} cy={12} r={40} />
        <circle cx={-50} cy={-30} r={52} />
        <circle cx={40} cy={-28} r={50} />
        <ellipse cx={0} cy={16} rx={150} ry={46} />
      </g>
      <ellipse cx={0} cy={10} rx={146} ry={40} fill="#DDE3EE" />
      <rect x={-104} y={-18} width={208} height={62} rx={6} fill="#0A0616" stroke={INK} strokeWidth={4} />
      <text x={0} y={lines.length > 1 ? 8 : 22} textAnchor="middle" fontSize={17} fill={glitch ? "#FF3B3B" : CYAN} style={{ fontFamily: "var(--font-bangers), Impact, sans-serif", letterSpacing: 1.5 }}>
        {lines.map((l, i) => (
          <tspan key={i} x={glitch && i === 1 ? 6 : 0} dy={i === 0 ? 0 : 22}>
            {l}
          </tspan>
        ))}
      </text>
      {glitch && (
        <g>
          <rect x={-104} y={4} width={208} height={5} fill={MAGENTA} opacity={0.7} />
          <rect x={-60} y={26} width={120} height={4} fill={CYAN} opacity={0.7} />
        </g>
      )}
      <text x={0} y={-50} textAnchor="middle" fontSize={20} fill={INK} style={{ fontFamily: "var(--font-bangers), Impact, sans-serif", letterSpacing: 3 }}>
        NIMBUS
      </text>
      <rect x={-30} y={58} width={60} height={16} fill="#5B6478" stroke={INK} strokeWidth={4} />
    </g>
  );
}

export function Crystal({ x, y, s = 1, glow = true }: { x: number; y: number; s?: number; glow?: boolean }) {
  const { url } = usePage();
  return (
    <g transform={`translate(${x} ${y}) scale(${s})`}>
      {glow && <circle r={48} fill={url("glowC")} />}
      <polygon points="0,-30 16,-12 16,14 0,30 -16,14 -16,-12" fill="#8FEAFF" stroke={INK} strokeWidth={4} strokeLinejoin="round" />
      <polygon points="0,-30 16,-12 0,-2 -16,-12" fill="#DFFAFF" />
      <polygon points="0,-2 16,-12 16,14 0,30" fill={CYAN} />
      <path d="M-6,-8 L-2,4" stroke="#fff" strokeWidth={3} strokeLinecap="round" />
    </g>
  );
}

export function Bookshelf({ x, y, w, h, seed = 3 }: { x: number; y: number; w: number; h: number; seed?: number }) {
  const r = rng(seed);
  const colors = [MAGENTA, YELLOW, CYAN, "#FF7A00", "#7B5CFF", "#F5EFE3", "#2FD17A"];
  const shelves: React.ReactNode[] = [];
  const rowH = 58;
  for (let sy = y + 10; sy < y + h - 20; sy += rowH) {
    let bx = x + 8;
    while (bx < x + w - 14) {
      const bw = 8 + r() * 12;
      const bh = 30 + r() * 18;
      const lean = r() > 0.9;
      shelves.push(
        <rect
          key={`${bx}-${sy}`}
          x={bx}
          y={sy + rowH - 10 - bh}
          width={bw}
          height={bh}
          fill={colors[Math.floor(r() * colors.length)]}
          stroke={INK}
          strokeWidth={2}
          transform={lean ? `rotate(12 ${bx} ${sy + rowH - 10})` : undefined}
        />,
      );
      bx += bw + (lean ? 8 : 1);
    }
    shelves.push(<rect key={`s${sy}`} x={x} y={sy + rowH - 10} width={w} height={8} fill="#4A2E19" stroke={INK} strokeWidth={3} />);
  }
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} fill="#2A1A0F" stroke={INK} strokeWidth={4} />
      {shelves}
    </g>
  );
}

export function RetroPC({ x, y, s = 1, glow = true }: { x: number; y: number; s?: number; glow?: boolean }) {
  const { url } = usePage();
  return (
    <g transform={`translate(${x} ${y}) scale(${s})`}>
      {glow && <circle cx={0} cy={-60} r={110} fill={url("glowC")} opacity={0.35} />}
      <rect x={-90} y={-130} width={180} height={130} rx={8} fill="#D9D2C0" stroke={INK} strokeWidth={5} />
      <rect x={-76} y={-118} width={152} height={100} fill="#0E1426" stroke={INK} strokeWidth={4} />
      {/* tiny comic page on screen */}
      <rect x={-40} y={-112} width={60} height={88} fill="#F5EFE3" stroke={INK} strokeWidth={2} />
      <rect x={-36} y={-108} width={52} height={32} fill={MAGENTA} stroke={INK} strokeWidth={1.5} />
      <rect x={-36} y={-72} width={24} height={44} fill={YELLOW} stroke={INK} strokeWidth={1.5} />
      <rect x={-10} y={-72} width={26} height={44} fill={CYAN} stroke={INK} strokeWidth={1.5} />
      <rect x={26} y={-112} width={44} height={88} fill="#1B2240" stroke={CYAN} strokeWidth={1.5} />
      <text x={48} y={-92} fontSize={9} fill={CYAN} textAnchor="middle" style={{ fontFamily: "var(--font-bangers), Impact, sans-serif" }}>
        KOMIK
      </text>
      <rect x={32} y={-84} width={32} height={4} fill={YELLOW} />
      <rect x={32} y={-76} width={24} height={4} fill="#fff" opacity={0.6} />
      <rect x={-30} y={0} width={60} height={14} fill="#BDB5A2" stroke={INK} strokeWidth={4} />
      <rect x={-110} y={14} width={220} height={22} rx={4} fill="#D9D2C0" stroke={INK} strokeWidth={5} />
    </g>
  );
}

export function Rooftop({ w, y, color = "#0A0816" }: { w: number; y: number; color?: string }) {
  return (
    <g>
      <rect x={-10} y={y} width={w + 20} height={400} fill={color} stroke={INK} strokeWidth={4} />
      <rect x={-10} y={y} width={w + 20} height={10} fill="#2A2344" stroke={INK} strokeWidth={3} />
      <rect x={w * 0.12} y={y - 38} width={46} height={38} fill="#1A1530" stroke={INK} strokeWidth={3} />
      <path d={`M${w * 0.12 + 6},${y - 38} l6,-16 h22 l6,16`} fill="#2A2344" stroke={INK} strokeWidth={3} />
      <line x1={w * 0.72} y1={y} x2={w * 0.72} y2={y - 60} stroke={INK} strokeWidth={4} />
      <path d={`M${w * 0.72 - 16},${y - 60} L${w * 0.72 + 16},${y - 60} L${w * 0.72},${y - 76} Z`} fill="#2A2344" stroke={INK} strokeWidth={3} />
    </g>
  );
}

export function Laser({ x1, y1, x2, y2, color = "#FF2A2A" }: { x1: number; y1: number; x2: number; y2: number; color?: string }) {
  return (
    <g strokeLinecap="round">
      <line x1={x1} y1={y1} x2={x2} y2={y2} stroke={color} strokeWidth={16} opacity={0.25} />
      <line x1={x1} y1={y1} x2={x2} y2={y2} stroke={color} strokeWidth={7} />
      <line x1={x1} y1={y1} x2={x2} y2={y2} stroke="#FFE3E3" strokeWidth={2.5} />
    </g>
  );
}

export function SyncRings({ cx, cy, count = 5, gap = 60, color = "#FF2A2A" }: { cx: number; cy: number; count?: number; gap?: number; color?: string }) {
  return (
    <g fill="none">
      {Array.from({ length: count }).map((_, i) => (
        <circle key={i} cx={cx} cy={cy} r={30 + i * gap} stroke={color} strokeWidth={6 - i * 0.8} opacity={0.85 - i * 0.14} strokeDasharray={i % 2 ? "18 10" : undefined} />
      ))}
    </g>
  );
}

export function Debris({ cx, cy, count = 14, spread = 160, seed = 8 }: { cx: number; cy: number; count?: number; spread?: number; seed?: number }) {
  const r = rng(seed);
  return (
    <g>
      {Array.from({ length: count }).map((_, i) => {
        const a = r() * Math.PI * 2;
        const d = spread * (0.3 + r() * 0.7);
        const x = cx + Math.cos(a) * d;
        const y = cy + Math.sin(a) * d;
        const s = 8 + r() * 16;
        return <rect key={i} x={x} y={y} width={s * 1.6} height={s} fill="#6B3A4A" stroke={INK} strokeWidth={3} transform={`rotate(${r() * 90} ${x} ${y})`} />;
      })}
    </g>
  );
}
