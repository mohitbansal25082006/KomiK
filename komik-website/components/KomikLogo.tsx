import React from "react";

interface KomikLogoProps {
  size?: number;
  className?: string;
}

export default function KomikLogo({ size = 32, className = "" }: KomikLogoProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 100 100"
      width={size}
      height={size}
      fill="none"
      className={`shrink-0 select-none ${className}`}
      aria-label="Komik Logo"
    >
      {/* Global Comic Drop Shadow */}
      <g transform="translate(3.5, 4)">
        {/* Base book shadow */}
        <polygon
          points="13,74 50,83 87,74 87,78 50,87 13,78"
          fill="#000000"
          opacity="0.85"
        />
        {/* Left cover shadow */}
        <polygon
          points="14,20 48,28 48,78 14,70"
          fill="#000000"
          opacity="0.85"
        />
        {/* Right page shadow */}
        <polygon
          points="52,28 86,20 86,70 52,78"
          fill="#000000"
          opacity="0.85"
        />
        {/* Ribbon shadow */}
        <polygon
          points="47,23 53,24 53,89 50,85 47,89"
          fill="#000000"
          opacity="0.7"
        />
      </g>

      {/* Book Under-Spine Structure */}
      <polygon
        points="13,74 50,83 87,74 87,78 50,87 13,78"
        fill="#16171D"
        stroke="#000000"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />

      {/* Left Cover (Comic Front Cover) */}
      <polygon
        points="14,20 48,28 48,78 14,70"
        fill="#0E0F14"
        stroke="#000000"
        strokeWidth="3.5"
        strokeLinejoin="round"
      />

      {/* Left Cover Inset Field (Amber / Gold) */}
      <polygon
        points="18,24 44,30 44,74 18,67"
        fill="#FFD700"
        stroke="#000000"
        strokeWidth="2.2"
        strokeLinejoin="round"
      />

      {/* Bold Comic "K" Lettermark on Left Cover */}
      {/* K Stem */}
      <polygon
        points="23,34 29,35 29,63 23,62"
        fill="#000000"
      />
      {/* K Upper Arm */}
      <polygon
        points="29,48 39,36 43,37 32,51"
        fill="#000000"
      />
      {/* K Lower Arm */}
      <polygon
        points="31,47 43,64 38,64 27,49"
        fill="#000000"
      />

      {/* Comic Cover Header Line */}
      <line
        x1="22"
        y1="27"
        x2="40"
        y2="30"
        stroke="#000000"
        strokeWidth="1.8"
        strokeLinecap="round"
      />

      {/* Right Page (Crisp White Newsprint Page) */}
      <polygon
        points="52,28 86,20 86,70 52,78"
        fill="#FCFAF6"
        stroke="#000000"
        strokeWidth="3.5"
        strokeLinejoin="round"
      />

      {/* Top Action Panel (Cyan) */}
      <polygon
        points="56,33 82,27 82,47 56,52"
        fill="#00A3E0"
        stroke="#000000"
        strokeWidth="2"
        strokeLinejoin="round"
      />
      {/* White Action Speedlines in Cyan Panel */}
      <line
        x1="60"
        y1="37"
        x2="78"
        y2="33"
        stroke="#FFFFFF"
        strokeWidth="1.8"
        strokeLinecap="round"
        opacity="0.9"
      />
      <line
        x1="60"
        y1="43"
        x2="74"
        y2="40"
        stroke="#FFFFFF"
        strokeWidth="1.8"
        strokeLinecap="round"
        opacity="0.9"
      />

      {/* Bottom Splash Panel (Magenta) */}
      <polygon
        points="56,55 82,49 82,65 56,71"
        fill="#E60050"
        stroke="#000000"
        strokeWidth="2"
        strokeLinejoin="round"
      />
      {/* White Dialogue Line in Magenta Panel */}
      <line
        x1="60"
        y1="59"
        x2="78"
        y2="55"
        stroke="#FFFFFF"
        strokeWidth="1.8"
        strokeLinecap="round"
        opacity="0.9"
      />

      {/* Center Spine Seam */}
      <line
        x1="50"
        y1="27"
        x2="50"
        y2="81"
        stroke="#000000"
        strokeWidth="4"
        strokeLinecap="round"
      />

      {/* Hanging Bookmark Ribbon in Crimson */}
      <polygon
        points="47,23 53,24 53,89 50,85 47,89"
        fill="#E60050"
        stroke="#000000"
        strokeWidth="2"
        strokeLinejoin="round"
      />
    </svg>
  );
}
