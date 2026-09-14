"use client";

import React from "react";
import {
  Balloon,
  Burst,
  Caption,
  ComicPageFrame,
  CYAN,
  FONT_LETTER,
  FONT_SFX,
  Halftone,
  INK,
  MAGENTA,
  MotionLines,
  NIGHT,
  PAPER,
  Panel,
  Rain,
  Sfx,
  SpeedLines,
  YELLOW,
  rng,
  usePage,
} from "./kit";
import {
  Blimp,
  Bookshelf,
  Byte,
  Citizen,
  CloudHelmet,
  Crystal,
  Debris,
  EnemyDrone,
  Laser,
  Moon,
  NeonSign,
  Orrin,
  OrrinFace,
  RetroPC,
  Rooftop,
  Sky,
  Skyline,
  Stars,
  Stratus,
  StratusFace,
  StratusHelmetFace,
  SyncRings,
  Vex,
  VexFace,
} from "./art";

/* ------------------------------------------------------------------ */
/*  CYBERPUNK CHRONICLES #01 — "THE LAST LOCAL ARCHIVE"                */
/*  An original 12-page demo comic written for the Komik website.      */
/*  All lettering lives in SCRIPT so the reader's offline "OCR" search */
/*  can search real dialogue.                                          */
/* ------------------------------------------------------------------ */

export const COMIC_TITLE = "Cyberpunk Chronicles #01";
export const COMIC_FILE = "Cyberpunk_Chronicles_#01.cbz";

export const SCRIPT: Record<number, Record<string, string>> = {
  1: {
    title: "CYBERPUNK",
    sub: "CHRONICLES",
    tag: "THE LAST LOCAL ARCHIVE",
    burst: "FIRST\nISSUE!",
  },
  2: {
    cap1: "NEON SPRAWL, 2089.",
    cap2: "NIMBUS CORP OWNS THE SKY...\nAND EVERY STORY EVER WRITTEN.",
    citizen: "MY WHOLE COMIC\nCOLLECTION...\nGONE?!",
    kid: "BUT I PAID\nFOR THEM!",
    cap3: "BUT NOT EVERYONE LOGGED IN.",
    vex: "COME ON, BYTE.\nORRIN'S SIGNAL CAME\nFROM THE OLD DISTRICT.",
    byte: "BEEP! ROUTE\nCACHED OFFLINE.",
  },
  3: {
    cap1: "THE INK VAULT. LAST SHOP IN THE\nCITY THAT STILL SELLS PAPER.",
    orrin1: "KIRA. YOU CAME.\nTHEY'VE FOUND US.",
    vex1: "WHO FOUND\nUS? NIMBUS?",
    orrin2: "THIS IS THE LAST LOCAL\nARCHIVE. EVERY COMIC\nEVER DRAWN. NO SERVERS.\nNO ACCOUNTS.",
    vex2: "WHY ME?",
    orrin3: "YOU NEVER SIGNED\nTHEIR TERMS.",
  },
  4: {
    stratus: "BY ORDER OF NIMBUS CORP:\nALL LOCAL FILES MUST\nBE UPLOADED!",
    drone: "SCANNING FOR\nOFFLINE CONTENT...",
    orrin: "KIRA! TAKE IT\nAND RUN!!",
    byte: "EXIT ROUTE\nFOUND!",
  },
  5: {
    cap1: "SECTOR 9 ROOFTOPS. 00:42.",
    byte: "BZZT! SIX NIMBUS\nDRONES ON OUR\nTAIL!",
    vex1: "THEY CAN TRACK ANYTHING\nTHAT'S CONNECTED...",
    vex2: "...SO WE STAY\nDISCONNECTED!",
  },
  6: {
    cap1: "NIMBUS TOWER. 300 FLOORS UP.",
    stratus1: "HAND OVER THE\nARCHIVE, COURIER.",
    stratus2: "RESISTANCE REQUIRES\nA PREMIUM PLAN.",
    vex1: "FUNNY.",
    vex2: "I ACTUALLY READ\nTHE FINE PRINT.",
    stratus3: "NO!!",
    byte: "WHEEEE!",
  },
  7: {
    cap1: "SOMETIMES THE ONLY WAY OUT...",
    cap2: "...IS OFFLINE.",
  },
  8: {
    stratus: "ACTIVATE THE SYNC!\nPULL EVERY FILE\nIN RANGE!",
    byte1: "THEY'RE SYNCING\nTHE ARCHIVE\nREMOTELY!",
    vex: "BYTE! AIRPLANE\nMODE. NOW!!",
    byte2: "WI-FI OFF. BLUETOOTH\nOFF. WE'RE A GHOST!",
    error: "SYNC FAILED: DEVICE NOT FOUND",
    cap1: "YOU CAN'T STEAL WHAT YOU CAN'T REACH.",
  },
  9: {
    stratus1: "THEN I'LL TAKE\nIT BY HAND!",
    vex1: "OOF!",
    vex2: "BYTE! NIGHT MODE,\nMAXIMUM WARMTH!",
    byte: "LUT LOADED!",
    stratus2: "AAGH! TOO...\nWARM!!",
  },
  10: {
    cap1: "SILENCE. JUST THE RAIN.",
    stratus1: "I USED TO READ\nCOMICS AS A KID...",
    stratus2: "NOW I CAN'T\nOPEN ONE WITHOUT\nA LOGIN.",
    vex1: "THEN READ ONE.\nRIGHT HERE.",
    vex2: "NO ACCOUNT.\nNO ONE WATCHING.",
  },
  11: {
    cap1: "BY DAWN, THE ARCHIVE\nWAS COPIED TO A THOUSAND\nOFFLINE MACHINES.",
    kid: "IT OPENS\nINSTANTLY!",
    orrin: "EVERY PAGE, RIGHT\nWHERE IT BELONGS.",
    vex: "ON YOUR OWN\nMACHINE.",
    byte: "SAVED LOCALLY!",
    cap2: "THE END... OF ISSUE #01",
  },
  12: {
    end: "THE END",
    next: "NEXT ISSUE: #02 WEBTOON WARS",
    ad: "READ THIS COMIC IN KOMIK",
  },
};

export const PAGE_TITLES: Record<number, string> = {
  1: "Cover",
  2: "Neon Sprawl, 2089",
  3: "The Ink Vault",
  4: "Kra-Koom!",
  5: "Rooftop Chase",
  6: "The Fine Print",
  7: "...Is Offline",
  8: "The Sync",
  9: "Night Mode",
  10: "Unmasked",
  11: "Dawn",
  12: "Back Cover",
};

/* ------------------------- small helpers -------------------------- */

function Tower({ x, y, w, h, seed, color = "#120F2A" }: { x: number; y: number; w: number; h: number; seed: number; color?: string }) {
  const r = rng(seed);
  const wins: React.ReactNode[] = [];
  const cols = [YELLOW, CYAN, MAGENTA, "#FFF1B8"];
  for (let wy = y + 16; wy < y + h - 10; wy += 22) {
    for (let wx = x + 10; wx < x + w - 12; wx += 16) {
      if (r() > 0.55) wins.push(<rect key={`${wx}-${wy}`} x={wx} y={wy} width={7} height={11} fill={cols[Math.floor(r() * 4)]} opacity={0.85} />);
    }
  }
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} fill={color} stroke={INK} strokeWidth={4} />
      {wins}
    </g>
  );
}

function Bricks({ w, h, opacity = 1 }: { w: number; h: number; opacity?: number }) {
  const { url } = usePage();
  return <rect width={w} height={h} fill={url("bricks")} opacity={opacity} />;
}

function Barcode({ x, y, w = 110, h = 70 }: { x: number; y: number; w?: number; h?: number }) {
  const r = rng(99);
  const bars: React.ReactNode[] = [];
  let bx = x + 8;
  while (bx < x + w - 10) {
    const bw = 1.5 + Math.floor(r() * 3) * 1.5;
    bars.push(<rect key={bx} x={bx} y={y + 8} width={bw} height={h - 28} fill={INK} />);
    bx += bw + 1.5 + r() * 2;
  }
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} fill="#fff" stroke={INK} strokeWidth={3} />
      {bars}
      <text x={x + w / 2} y={y + h - 7} fontSize={11} textAnchor="middle" fill={INK} style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
        0 72945 00001 1
      </text>
    </g>
  );
}

/* ============================== PAGES ============================= */

function Page1() {
  const T = SCRIPT[1];
  return (
    <ComicPageFrame number={1} bleed>
      <Sky w={600} h={900} kind="skyNeon" />
      <Stars w={600} h={500} count={70} seed={12} />
      <Moon cx={300} cy={400} r={150} />
      <Blimp x={470} y={270} s={0.55} screen={"OWN NOTHING."} />
      <Skyline w={600} baseY={900} seed={7} minH={180} maxH={440} color="#120F2A" minW={50} maxW={110} />
      <Rain w={600} h={900} count={120} opacity={0.25} seed={31} />
      <SpeedLines cx={300} cy={560} w={600} h={900} count={60} color={CYAN} opacity={0.12} inner={200} />
      <Rooftop w={600} y={770} color="#07060F" />
      <Vex pose="heroic" x={300} y={770} s={1.55} />
      <Byte x={430} y={560} s={0.95} mood="normal" />

      {/* top trade dress */}
      <rect x={0} y={0} width={600} height={36} fill={YELLOW} stroke={INK} strokeWidth={4} />
      <rect x={10} y={5} width={50} height={26} fill={INK} />
      <text x={35} y={25} textAnchor="middle" fontSize={20} fill={YELLOW} style={{ fontFamily: FONT_SFX }}>
        #01
      </text>
      <text x={300} y={26} textAnchor="middle" fontSize={20} fill={INK} style={{ fontFamily: FONT_SFX, letterSpacing: 4 }}>
        KOMIK COMICS PRESENTS
      </text>
      <text x={585} y={25} textAnchor="end" fontSize={16} fill={INK} style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
        $3.99
      </text>

      {/* masthead */}
      <Sfx x={300} y={146} text={T.title} size={110} fill={YELLOW} shadow={MAGENTA} rotate={-3} />
      <g transform="rotate(-3 300 185)">
        <rect x={130} y={162} width={340} height={46} fill={MAGENTA} stroke={INK} strokeWidth={5} />
        <text x={300} y={197} textAnchor="middle" fontSize={38} fill="#fff" style={{ fontFamily: FONT_SFX, letterSpacing: 10 }}>
          {T.sub}
        </text>
      </g>

      <Burst cx={96} cy={290} r={62} fill={CYAN} spikes={14} seed={4} />
      <text x={96} y={284} textAnchor="middle" fontSize={25} fill={INK} style={{ fontFamily: FONT_SFX }} transform="rotate(-10 96 290)">
        {T.burst.split("\n").map((l, i) => (
          <tspan key={i} x={96} dy={i === 0 ? 0 : 26}>
            {l}
          </tspan>
        ))}
      </text>

      {/* tagline + barcode */}
      <rect x={34} y={812} width={400} height={62} fill={PAPER} stroke={INK} strokeWidth={5} />
      <rect x={34} y={812} width={400} height={62} fill="none" stroke={MAGENTA} strokeWidth={2} transform="translate(5 5)" />
      <text x={234} y={855} textAnchor="middle" fontSize={34} fill={INK} style={{ fontFamily: FONT_SFX, letterSpacing: 2 }}>
        {T.tag}
      </text>
      <Barcode x={464} y={800} />
    </ComicPageFrame>
  );
}

function Page2() {
  const T = SCRIPT[2];
  return (
    <ComicPageFrame number={2}>
      <Panel x={24} y={24} w={552} h={300}>
        <Sky w={552} h={300} kind="skyNeon" />
        <Stars w={552} h={200} count={40} />
        <Moon cx={480} cy={70} r={36} />
        <Skyline w={552} baseY={300} seed={11} minH={80} maxH={200} color="#140F2E" />
        <Blimp x={250} y={112} s={0.82} />
        <Caption x={14} y={14} text={T.cap1} size={17} />
        <Caption x={248} y={234} text={T.cap2} size={15} />
      </Panel>

      <Panel x={24} y={338} w={269} h={250} bg="#1A1233">
        <Skyline w={269} baseY={160} seed={13} minH={40} maxH={120} color="#241B45" />
        <rect y={190} width={269} height={60} fill="#0D0A1C" />
        <Halftone w={269} h={250} kind="dotsC" opacity={0.4} />
        <Citizen x={70} y={252} s={0.9} seed={1} />
        <Citizen x={200} y={252} s={1} flip seed={2} color="#3B2A55" />
        <Balloon x={140} y={58} text={T.citizen} size={17} tail={[80, 150]} />
      </Panel>

      <Panel x={307} y={338} w={269} h={250} bg="#0A0616">
        <rect x={20} y={24} width={229} height={132} fill="#1A0010" stroke="#FF3B3B" strokeWidth={4} />
        <text x={134} y={80} textAnchor="middle" fontSize={34} fill="#FF3B3B" style={{ fontFamily: FONT_SFX, letterSpacing: 2 }}>
          ACCESS REVOKED
        </text>
        <text x={134} y={108} textAnchor="middle" fontSize={13} fill="#fff" style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
          SUBSCRIPTION EXPIRED
        </text>
        <text x={134} y={128} textAnchor="middle" fontSize={13} fill="#FFB3B3" style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
          PLEASE LOG IN TO CONTINUE
        </text>
        <Sfx x={210} y={30} text="BZZT!" size={40} fill="#FF3B3B" shadow={INK} rotate={10} />
        <Citizen x={56} y={272} s={0.8} seed={5} screen={false} color="#2E4A7A" />
        <Balloon x={178} y={200} text={T.kid} size={17} tail={[92, 190]} />
      </Panel>

      <Panel x={24} y={602} w={552} h={248}>
        <Sky w={552} h={248} kind="skyNight" />
        <Stars w={552} h={160} count={50} seed={8} />
        <Moon cx={96} cy={96} r={46} />
        <Skyline w={552} baseY={248} seed={21} minH={30} maxH={110} color="#120F2A" />
        <Rooftop w={552} y={206} />
        <Vex pose="heroic" back x={440} y={206} s={0.82} />
        <Byte x={330} y={176} s={0.55} />
        <Caption x={14} y={14} text={T.cap3} size={16} />
        <Balloon x={260} y={96} text={T.vex} size={16} tail={[420, 70]} />
        <Balloon x={176} y={196} text={T.byte} size={14} kind="robot" tail={[300, 176]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page3() {
  const T = SCRIPT[3];
  return (
    <ComicPageFrame number={3}>
      <Panel x={24} y={24} w={552} h={260} bg="#120B24">
        <Bricks w={552} h={260} />
        <rect x={150} y={0} width={300} height={260} fill="#000" opacity={0.25} />
        <rect x={176} y={70} width={250} height={200} fill="#2A1A0F" stroke={INK} strokeWidth={5} />
        <Bookshelf x={192} y={112} w={116} h={126} seed={4} />
        <rect x={192} y={112} width={116} height={126} fill="#FFD700" opacity={0.12} />
        <rect x={322} y={120} width={86} height={150} fill="#3B2412" stroke={INK} strokeWidth={4} />
        <rect x={336} y={136} width={58} height={50} fill={YELLOW} stroke={INK} strokeWidth={3} opacity={0.9} />
        <circle cx={400} cy={200} r={4} fill={YELLOW} />
        <NeonSign x={300} y={42} text="THE INK VAULT" color={YELLOW} size={24} />
        <Rain w={552} h={260} count={80} />
        <Vex pose="stand" x={480} y={258} s={0.78} flip />
        <Caption x={14} y={196} text={T.cap1} size={15} />
      </Panel>

      <Panel x={24} y={298} w={330} h={290} bg="#2A1A0F">
        <Bookshelf x={-4} y={-4} w={338} h={298} seed={7} />
        <rect width={330} height={290} fill="#000" opacity={0.35} />
        <Orrin pose="give" x={150} y={300} s={1.05} />
        <Crystal x={206} y={184} s={0.7} />
        <Balloon x={150} y={50} text={T.orrin1} size={18} tail={[160, 112]} />
      </Panel>

      <Panel x={368} y={298} w={208} h={290} bg="#3B0F4F">
        <SpeedLines cx={104} cy={170} w={208} h={290} color={MAGENTA} opacity={0.35} inner={60} />
        <VexFace x={104} y={196} s={0.78} expr="worried" />
        <Balloon x={104} y={46} text={T.vex1} size={17} tail={[104, 100]} />
      </Panel>

      <Panel x={24} y={602} w={340} h={248} bg="#2A1A0F">
        <Bookshelf x={-4} y={-4} w={348} h={256} seed={12} />
        <rect width={340} height={248} fill="#000" opacity={0.4} />
        <Orrin pose="give" x={112} y={262} s={0.95} />
        <Crystal x={162} y={156} s={0.62} />
        <Balloon x={184} y={58} text={T.orrin2} size={16} tail={[132, 118]} />
      </Panel>

      <Panel x={378} y={602} w={198} h={248} bg="#1B1030">
        <Halftone w={198} h={248} kind="dotsC" opacity={0.5} />
        <VexFace x={99} y={150} s={0.7} expr="neutral" />
        <Balloon x={99} y={34} text={T.vex2} size={18} tail={[99, 72]} />
        <Balloon x={104} y={222} text={T.orrin3} size={15} tail={[-10, 236]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page4() {
  const T = SCRIPT[4];
  return (
    <ComicPageFrame number={4}>
      <Panel x={24} y={24} w={552} h={330} bg="#3A1F2B">
        <Bricks w={552} h={330} />
        <Burst cx={290} cy={170} r={170} fill={YELLOW} spikes={18} seed={21} />
        <Burst cx={290} cy={170} r={108} fill="#FF7A00" spikes={14} seed={22} />
        <Burst cx={290} cy={170} r={56} fill="#FFF4B0" spikes={10} seed={23} />
        <Debris cx={290} cy={170} spread={250} count={18} />
        <EnemyDrone x={110} y={80} s={0.9} />
        <EnemyDrone x={470} y={96} s={0.8} flip />
        <Sfx x={290} y={200} text="KRA-KOOM!!" size={90} fill={YELLOW} shadow={MAGENTA} rotate={-6} />
      </Panel>

      <Panel x={24} y={368} w={552} h={250}>
        <rect width={552} height={250} fill="#0D0B22" />
        <ellipse cx={180} cy={150} rx={170} ry={150} fill="#1E1348" stroke={INK} strokeWidth={5} />
        <Halftone w={552} h={250} kind="dotsW" opacity={0.6} />
        <EnemyDrone x={60} y={48} s={0.55} />
        <Stratus pose="point" x={170} y={252} s={0.95} />
        <Balloon x={392} y={100} text={T.stratus} size={17} kind="shout" tail={[222, 70]} seed={2} />
        <EnemyDrone x={500} y={170} s={0.55} flip />
        <Balloon x={374} y={212} text={T.drone} size={14} kind="robot" tail={[480, 172]} />
      </Panel>

      <Panel x={24} y={632} w={269} h={218} bg="#3B0F4F">
        <SpeedLines cx={134} cy={150} w={269} h={218} color={MAGENTA} opacity={0.4} inner={70} />
        <OrrinFace x={134} y={150} s={0.6} expr="shout" />
        <Balloon x={134} y={42} text={T.orrin} size={17} kind="shout" tail={[140, 96]} seed={5} />
      </Panel>

      <Panel x={307} y={632} w={269} h={218} bg="#0D0B22">
        <rect x={160} y={18} width={100} height={150} fill="#1E1348" stroke={INK} strokeWidth={5} />
        <polygon points="160,18 200,60 180,90 160,70" fill="#BFE9FF" stroke={INK} strokeWidth={2} opacity={0.8} />
        <polygon points="260,18 230,50 250,110 260,100" fill="#BFE9FF" stroke={INK} strokeWidth={2} opacity={0.8} />
        <polygon points="200,168 215,120 240,168" fill="#BFE9FF" stroke={INK} strokeWidth={2} opacity={0.8} />
        <Vex pose="leap" x={140} y={180} s={0.72} />
        <Crystal x={212} y={78} s={0.38} />
        <Sfx x={214} y={200} text="KSSH!" size={42} fill={CYAN} rotate={8} />
        <Byte x={42} y={48} s={0.5} mood="alarm" />
        <Balloon x={78} y={150} text={T.byte} size={14} kind="robot" tail={[46, 70]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page5() {
  const T = SCRIPT[5];
  return (
    <ComicPageFrame number={5}>
      <Panel x={24} y={24} w={552} h={300}>
        <Sky w={552} h={300} kind="skyNeon" />
        <Stars w={552} h={200} count={40} seed={15} />
        <Skyline w={552} baseY={300} seed={33} minH={40} maxH={150} color="#1B1340" />
        <rect x={-10} y={200} width={200} height={120} fill="#0A0816" stroke={INK} strokeWidth={4} />
        <rect x={360} y={180} width={210} height={140} fill="#0A0816" stroke={INK} strokeWidth={4} />
        <MotionLines x={40} y={60} w={150} count={6} spacing={14} />
        <Vex pose="leap" x={290} y={182} s={1} />
        <Sfx x={450} y={262} text="WHOOSH!" size={58} fill={CYAN} rotate={-4} />
        <Caption x={14} y={14} text={T.cap1} size={15} />
      </Panel>

      <Panel x={24} y={338} w={269} h={240} bg="#2B0A12">
        <SpeedLines cx={134} cy={160} w={269} h={240} color="#FF3B3B" opacity={0.35} inner={60} />
        <Byte x={134} y={168} s={1.5} mood="alarm" />
        <Balloon x={134} y={50} text={T.byte} size={16} kind="robot" tail={[134, 98]} />
      </Panel>

      <Panel x={307} y={338} w={269} h={240} bg="#0D0B22">
        <Skyline w={269} baseY={240} seed={5} minH={30} maxH={90} color="#151230" />
        <Laser x1={60} y1={70} x2={20} y2={240} />
        <Laser x1={200} y1={50} x2={269} y2={220} />
        <Laser x1={150} y1={120} x2={116} y2={240} />
        <EnemyDrone x={60} y={62} s={0.7} />
        <EnemyDrone x={200} y={42} s={0.6} flip />
        <EnemyDrone x={150} y={112} s={0.8} />
        <Sfx x={70} y={186} text="ZAP!" size={44} fill="#FF3B3B" shadow={YELLOW} rotate={-12} />
        <Sfx x={214} y={158} text="ZAP!" size={36} fill="#FF3B3B" shadow={YELLOW} rotate={10} />
      </Panel>

      <Panel x={24} y={592} w={552} h={258}>
        <Sky w={552} h={258} kind="skyNight" />
        <Skyline w={552} baseY={212} seed={44} minH={40} maxH={120} color="#120F2A" />
        <Rooftop w={552} y={212} />
        <Laser x1={560} y1={70} x2={190} y2={150} />
        <Vex pose="slide" x={250} y={212} s={1.1} />
        <MotionLines x={20} y={160} w={140} count={4} spacing={10} />
        <Balloon x={150} y={58} text={T.vex1} size={17} tail={[290, 140]} />
        <Balloon x={440} y={156} text={T.vex2} size={17} tail={[330, 170]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page6() {
  const T = SCRIPT[6];
  return (
    <ComicPageFrame number={6}>
      <Panel x={24} y={24} w={552} h={280}>
        <Sky w={552} h={280} kind="skyNeon" />
        <Stars w={552} h={160} count={40} seed={61} />
        <Skyline w={552} baseY={280} seed={61} minH={20} maxH={80} color="#1B1340" minW={16} maxW={36} />
        <rect x={180} y={196} width={400} height={100} fill="#0A0816" stroke={INK} strokeWidth={5} />
        <Vex pose="heroic" x={232} y={196} s={0.82} />
        <Burst cx={452} cy={196} r={40} fill="#FF7A00" spikes={10} seed={3} />
        <Stratus pose="stand" x={452} y={196} s={0.9} flip />
        <Sfx x={470} y={260} text="THOOM!" size={42} fill="#FF7A00" rotate={-4} />
        <Caption x={14} y={14} text={T.cap1} size={15} />
      </Panel>

      <Panel x={24} y={318} w={300} h={270} bg="#2B0A12">
        <Halftone w={300} h={270} kind="dotsM" />
        <StratusHelmetFace x={150} y={178} s={0.7} />
        <Balloon x={150} y={46} text={T.stratus1} size={17} tail={[150, 96]} />
        <Balloon x={150} y={236} text={T.stratus2} size={15} tail={[150, 196]} />
      </Panel>

      <Panel x={338} y={318} w={238} h={270} bg="#3B0F4F">
        <SpeedLines cx={119} cy={160} w={238} h={270} color={MAGENTA} opacity={0.3} inner={70} />
        <VexFace x={119} y={170} s={0.7} expr="smirk" />
        <Balloon x={119} y={36} text={T.vex1} size={18} tail={[119, 76]} />
        <Balloon x={119} y={236} text={T.vex2} size={15} tail={[119, 196]} />
      </Panel>

      <Panel x={24} y={602} w={552} h={248}>
        <Sky w={552} h={248} kind="skyNight" />
        <Stars w={552} h={248} count={50} seed={66} />
        <Skyline w={552} baseY={260} seed={67} minH={10} maxH={60} color="#1B1340" minW={14} maxW={30} />
        <rect x={-10} y={228} width={300} height={40} fill="#0A0816" stroke={INK} strokeWidth={5} />
        <Stratus pose="reach" x={200} y={228} s={0.8} />
        <MotionLines x={330} y={70} w={80} count={5} spacing={12} angle={-30} />
        <Vex pose="flip" x={410} y={210} s={0.8} />
        <Byte x={490} y={60} s={0.55} mood="happy" />
        <Balloon x={110} y={66} text={T.stratus3} size={22} kind="shout" tail={[190, 92]} seed={9} />
        <Balloon x={480} y={130} text={T.byte} size={15} kind="robot" tail={[488, 88]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page7() {
  const T = SCRIPT[7];
  return (
    <ComicPageFrame number={7}>
      <Panel x={24} y={24} w={552} h={826}>
        <Sky w={552} h={826} kind="skyNeon" />
        <Stars w={552} h={400} count={60} seed={71} />
        <Skyline w={552} baseY={826} seed={71} minH={40} maxH={200} color="#1B1340" />
        <Tower x={-10} y={110} w={140} h={760} seed={72} />
        <Tower x={112} y={330} w={80} h={540} seed={73} color="#17123A" />
        <Tower x={430} y={70} w={140} h={800} seed={74} />
        <Tower x={372} y={420} w={66} h={450} seed={75} color="#17123A" />
        <NeonSign x={90} y={320} text="KOMIK" color={CYAN} size={28} vertical />
        <NeonSign x={482} y={300} text="OFFLINE" color={MAGENTA} size={22} vertical />
        <Rain w={552} h={826} count={220} opacity={0.4} seed={77} />
        <MotionLines x={300} y={120} w={130} count={9} spacing={16} angle={90} />
        <g stroke={INK} strokeWidth={4} strokeLinejoin="round">
          <path d="M210,272 L78,352 L262,344 Z" fill={CYAN} opacity={0.92} />
          <path d="M398,287 L534,362 L326,352 Z" fill={CYAN} opacity={0.92} />
        </g>
        <path d="M210,272 L262,344 M398,287 L326,352" stroke={INK} strokeWidth={3} />
        <Vex pose="glide" x={280} y={520} s={1.4} />
        <Byte x={160} y={450} s={0.7} mood="happy" />
        <Sfx x={290} y={700} text="FWOOSH!" size={96} fill={CYAN} shadow={MAGENTA} rotate={-8} />
        <Caption x={20} y={20} text={T.cap1} size={21} />
        <Caption x={300} y={760} text={T.cap2} size={24} bg={MAGENTA} color="#fff" />
      </Panel>
    </ComicPageFrame>
  );
}

function Page8() {
  const T = SCRIPT[8];
  return (
    <ComicPageFrame number={8}>
      <Panel x={24} y={24} w={552} h={270}>
        <Sky w={552} h={270} kind="skyNeon" />
        <Skyline w={552} baseY={270} seed={81} minH={40} maxH={120} color="#120F2A" />
        <SyncRings cx={390} cy={120} count={6} gap={70} />
        <rect x={330} y={152} width={120} height={200} fill="#0A0816" stroke={INK} strokeWidth={5} />
        <Stratus pose="shout" x={390} y={152} s={0.7} />
        <Sfx x={150} y={238} text="VRRRMMM" size={42} fill="#FF3B3B" shadow={YELLOW} rotate={-4} />
        <Balloon x={196} y={78} text={T.stratus} size={16} kind="shout" tail={[362, 50]} seed={4} />
      </Panel>

      <Panel x={24} y={308} w={269} h={260} bg="#2B0A12">
        <SpeedLines cx={134} cy={176} w={269} h={260} color="#FF3B3B" opacity={0.35} inner={60} />
        <Byte x={134} y={182} s={1.5} mood="alarm" />
        <Balloon x={134} y={54} text={T.byte1} size={16} kind="robot" tail={[134, 110]} />
      </Panel>

      <Panel x={307} y={308} w={269} h={260} bg="#3B2A00">
        <SpeedLines cx={134} cy={176} w={269} h={260} color={YELLOW} opacity={0.4} inner={60} />
        <VexFace x={134} y={188} s={0.66} expr="shout" />
        <Balloon x={134} y={48} text={T.vex} size={17} kind="shout" tail={[134, 108]} seed={6} />
      </Panel>

      <Panel x={24} y={582} w={552} h={268} bg="#120B24">
        <Bricks w={552} h={268} opacity={0.55} />
        <rect y={236} width={552} height={40} fill="#0A0816" />
        <SyncRings cx={720} cy={134} count={8} gap={88} />
        <Vex pose="heroic" x={150} y={262} s={0.95} />
        <Byte x={260} y={116} s={0.9} mood="plane" />
        <g>
          <rect x={330} y={152} width={206} height={70} fill="#1A0010" stroke="#FF3B3B" strokeWidth={4} />
          <text x={433} y={184} textAnchor="middle" fontSize={25} fill="#FF3B3B" style={{ fontFamily: FONT_SFX, letterSpacing: 1 }}>
            SYNC FAILED:
          </text>
          <text x={433} y={208} textAnchor="middle" fontSize={16} fill="#fff" style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
            DEVICE NOT FOUND
          </text>
        </g>
        <Balloon x={372} y={62} text={T.byte2} size={16} kind="robot" tail={[282, 104]} />
        <Caption x={208} y={230} text={T.cap1} size={14} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page9() {
  const T = SCRIPT[9];
  const { url } = usePage();
  return (
    <ComicPageFrame number={9}>
      <Panel x={24} y={24} w={552} h={250} bg="#120B24">
        <Bricks w={552} h={250} opacity={0.6} />
        <rect y={220} width={552} height={40} fill="#0A0816" />
        <Burst cx={380} cy={222} r={46} fill="#FF7A00" spikes={10} seed={91} />
        <Debris cx={380} cy={210} spread={90} count={8} seed={92} />
        <Stratus pose="stand" x={380} y={222} s={0.95} flip />
        <Vex pose="heroic" x={130} y={222} s={0.9} />
        <Sfx x={486} y={96} text="KRAK!" size={50} fill={YELLOW} rotate={8} />
        <Balloon x={240} y={52} text={T.stratus1} size={17} kind="shout" tail={[362, 54]} seed={7} />
      </Panel>

      <Panel x={24} y={288} w={269} h={250} bg={YELLOW}>
        <SpeedLines cx={150} cy={120} w={269} h={250} color="#FF7A00" opacity={0.6} inner={30} />
        <Burst cx={140} cy={128} r={38} fill="#fff" spikes={12} seed={93} />
        <Stratus pose="hit" x={196} y={252} s={0.8} flip />
        <Vex pose="punch" x={58} y={252} s={0.8} />
        <Sfx x={120} y={80} text="POW!" size={66} fill={MAGENTA} shadow={INK} rotate={-12} />
      </Panel>

      <Panel x={307} y={288} w={269} h={250} bg="#3B0F4F">
        <SpeedLines cx={110} cy={130} w={269} h={250} color={MAGENTA} opacity={0.4} inner={30} />
        <Vex pose="hit" x={80} y={252} s={0.8} />
        <Stratus pose="punch" x={206} y={252} s={0.8} flip />
        <Sfx x={172} y={70} text="WHAM!" size={56} fill={YELLOW} rotate={8} />
        <Balloon x={44} y={48} text={T.vex1} size={16} tail={[52, 116]} />
      </Panel>

      <Panel x={24} y={552} w={552} h={298} bg="#120B24">
        <circle cx={300} cy={150} r={280} fill={url("glowY")} />
        <SpeedLines cx={300} cy={150} w={552} h={298} color={YELLOW} opacity={0.7} inner={60} count={64} />
        <Burst cx={300} cy={150} r={110} fill="#FFF4B0" spikes={22} seed={94} strokeWidth={4} />
        <Byte x={300} y={152} s={1} mood="happy" />
        <Vex pose="heroic" x={90} y={300} s={0.95} />
        <Stratus pose="stagger" x={470} y={300} s={0.9} />
        <Sfx x={300} y={284} text="FLASSSH!" size={58} fill={YELLOW} shadow="#FF7A00" rotate={-3} />
        <Balloon x={150} y={48} text={T.vex2} size={16} kind="shout" tail={[100, 136]} seed={8} />
        <Balloon x={338} y={34} text={T.byte} size={15} kind="robot" tail={[306, 110]} />
        <Balloon x={452} y={112} text={T.stratus2} size={17} kind="shout" tail={[450, 150]} seed={10} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page10() {
  const T = SCRIPT[10];
  return (
    <ComicPageFrame number={10}>
      <Panel x={24} y={24} w={552} h={280} bg="#120B24">
        <Bricks w={552} h={280} opacity={0.5} />
        <rect y={240} width={552} height={40} fill="#0A0816" />
        <Stratus pose="kneel" masked={false} x={300} y={242} s={1} />
        <g transform="translate(450 236) rotate(24)">
          <CloudHelmet hx={0} hy={-20} r={24} cracked />
        </g>
        <Sfx x={470} y={170} text="CLANK" size={36} fill="#AEB6C8" shadow={INK} rotate={12} />
        <Vex pose="stand" x={86} y={290} s={1.15} />
        <Rain w={552} h={280} count={110} seed={101} />
        <Caption x={14} y={14} text={T.cap1} size={16} />
      </Panel>

      <Panel x={24} y={318} w={269} h={270} bg="#1B2240">
        <Rain w={269} h={270} count={40} opacity={0.3} seed={102} />
        <StratusFace x={134} y={186} s={0.7} expr="sad" />
        <Balloon x={134} y={46} text={T.stratus1} size={16} tail={[134, 104]} />
      </Panel>

      <Panel x={307} y={318} w={269} h={270} bg="#1B2240">
        <Rain w={269} h={270} count={40} opacity={0.3} seed={103} />
        <StratusFace x={134} y={196} s={0.7} expr="sad" flip />
        <Balloon x={134} y={56} text={T.stratus2} size={16} tail={[134, 118]} />
      </Panel>

      <Panel x={24} y={602} w={552} h={248} bg="#120B24">
        <Bricks w={552} h={248} opacity={0.4} />
        <rect y={236} width={552} height={20} fill="#0A0816" />
        <rect x={400} y={0} width={152} height={248} fill="#FFB547" opacity={0.18} />
        <Vex pose="reach" x={230} y={242} s={1} />
        <Stratus pose="kneel" masked={false} x={380} y={242} s={0.9} flip />
        <Balloon x={146} y={50} text={T.vex1} size={17} tail={[232, 92]} />
        <Balloon x={404} y={62} text={T.vex2} size={17} tail={[262, 90]} />
      </Panel>
    </ComicPageFrame>
  );
}

function Page11() {
  const T = SCRIPT[11];
  const { url } = usePage();
  return (
    <ComicPageFrame number={11}>
      <Panel x={24} y={24} w={552} h={300}>
        <Sky w={552} h={300} kind="skyDawn" />
        <circle cx={276} cy={300} r={200} fill={url("glowY")} />
        <circle cx={276} cy={300} r={100} fill="#FFE38A" stroke={INK} strokeWidth={5} />
        <Skyline w={552} baseY={300} seed={111} minH={50} maxH={170} color="#2B1B5A" windows={false} />
        <Blimp x={150} y={110} s={0.72} screen={"UPLOAD EVERY...\nERROR: NO SIGNAL"} glitch />
        <Caption x={297} y={14} text={T.cap1} size={15} />
      </Panel>

      <Panel x={24} y={338} w={269} h={250} bg="#2A1A0F">
        <Bookshelf x={-4} y={-4} w={277} h={140} seed={113} />
        <rect width={269} height={250} fill="#000" opacity={0.3} />
        <RetroPC x={150} y={206} s={0.92} />
        <Citizen x={44} y={268} s={0.72} screen={false} seed={9} color="#FF7A00" />
        <Citizen x={246} y={268} s={0.66} flip screen={false} seed={12} color="#2FD17A" />
        <Balloon x={86} y={44} text={T.kid} size={17} tail={[50, 176]} />
      </Panel>

      <Panel x={307} y={338} w={269} h={250} bg="#3B2412">
        <circle cx={134} cy={130} r={200} fill={url("glowY")} opacity={0.4} />
        <OrrinFace x={134} y={184} s={0.62} expr="smile" />
        <Balloon x={134} y={46} text={T.orrin} size={16} tail={[134, 112]} />
      </Panel>

      <Panel x={24} y={602} w={552} h={248}>
        <Sky w={552} h={248} kind="skyDawn" />
        <circle cx={420} cy={250} r={90} fill="#FFE38A" stroke={INK} strokeWidth={5} />
        <Skyline w={552} baseY={250} seed={121} minH={40} maxH={110} color="#2B1B5A" windows={false} />
        <Rooftop w={552} y={196} color="#1A1030" />
        <Vex pose="stand" back x={250} y={196} s={0.8} />
        <Byte x={320} y={110} s={0.7} mood="wink" />
        <Balloon x={140} y={56} text={T.vex} size={17} tail={[238, 70]} />
        <Balloon x={446} y={58} text={T.byte} size={15} kind="robot" tail={[344, 100]} />
        <Caption x={291} y={206} text={T.cap2} size={16} bg={MAGENTA} color="#fff" />
      </Panel>
    </ComicPageFrame>
  );
}

function Page12() {
  const T = SCRIPT[12];
  return (
    <ComicPageFrame number={12} bleed bg={NIGHT}>
      <Halftone w={600} h={900} kind="dotsM" opacity={0.6} />
      <SpeedLines cx={300} cy={300} w={600} h={900} count={70} color={MAGENTA} opacity={0.22} inner={120} />
      <Burst cx={300} cy={290} r={200} fill={YELLOW} spikes={22} seed={121} strokeWidth={7} />
      <Sfx x={300} y={330} text={T.end} size={124} fill="#fff" shadow={MAGENTA} rotate={-5} />

      <g transform="rotate(-2 300 540)">
        <rect x={60} y={500} width={480} height={70} fill={CYAN} stroke={INK} strokeWidth={5} />
        <text x={300} y={548} textAnchor="middle" fontSize={34} fill={INK} style={{ fontFamily: FONT_SFX, letterSpacing: 2 }}>
          {T.next}
        </text>
      </g>

      <rect x={40} y={618} width={330} height={200} fill={PAPER} stroke={INK} strokeWidth={5} />
      <rect x={40} y={618} width={330} height={44} fill={INK} />
      <text x={205} y={649} textAnchor="middle" fontSize={25} fill={YELLOW} style={{ fontFamily: FONT_SFX, letterSpacing: 2 }}>
        {T.ad}
      </text>
      <g style={{ fontFamily: FONT_LETTER }} fontWeight={700} fontSize={19} fill={INK}>
        <text x={62} y={696}>+ FREE &amp; OPEN SOURCE</text>
        <text x={62} y={726}>+ 100% OFFLINE, ZERO ACCOUNTS</text>
        <text x={62} y={756}>+ CBZ, CBR, CB7, PDF &amp; MORE</text>
        <text x={62} y={786}>+ WINDOWS 10 / 11</text>
      </g>

      <g>
        <rect x={396} y={618} width={170} height={200} fill="#3B0F4F" stroke={INK} strokeWidth={5} />
        <svg x={396} y={618} width={170} height={200} viewBox="0 0 170 200" overflow="hidden">
          <VexFace x={85} y={112} s={0.6} expr="smirk" />
        </svg>
        <rect x={396} y={618} width={170} height={200} fill="none" stroke={INK} strokeWidth={5} />
      </g>

      <Barcode x={456} y={836} w={110} h={54} />
      <text x={40} y={856} fontSize={13} fill="#fff" opacity={0.8} style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
        CYBERPUNK CHRONICLES IS AN ORIGINAL DEMO COMIC
      </text>
      <text x={40} y={876} fontSize={13} fill="#fff" opacity={0.8} style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
        CREATED FOR THE KOMIK WEBSITE. NO CLOUDS WERE HARMED.
      </text>
    </ComicPageFrame>
  );
}

export const COMIC_PAGES: React.ComponentType[] = [Page1, Page2, Page3, Page4, Page5, Page6, Page7, Page8, Page9, Page10, Page11, Page12];
export const TOTAL_PAGES = COMIC_PAGES.length;

/** Returns the flattened dialogue for a page (used by the OCR search & text overlay). */
export function pageText(n: number): string[] {
  const s = SCRIPT[n];
  if (!s) return [];
  return Object.values(s).map((t) => t.replace(/\n/g, " "));
}

export function ComicPage({ n }: { n: number }) {
  const C = COMIC_PAGES[n - 1];
  return C ? <C /> : null;
}
