// Tiny synthesized comic sound effects (no audio files): a "pop" for clicks, a "whoosh" when a
// download is queued and a bright "ding" when one finishes.

let ctx: AudioContext | null = null;
let enabled = true;

export function setSoundEnabled(on: boolean): void {
  enabled = on;
}

function audio(): AudioContext | null {
  if (!enabled) return null;
  try {
    ctx ??= new AudioContext();
    if (ctx.state === "suspended") void ctx.resume();
    return ctx;
  } catch {
    return null;
  }
}

function tone(freqFrom: number, freqTo: number, duration: number, type: OscillatorType, gain: number, delay = 0) {
  const a = audio();
  if (!a) return;
  const t = a.currentTime + delay;
  const osc = a.createOscillator();
  const g = a.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freqFrom, t);
  osc.frequency.exponentialRampToValueAtTime(Math.max(1, freqTo), t + duration);
  g.gain.setValueAtTime(gain, t);
  g.gain.exponentialRampToValueAtTime(0.0001, t + duration);
  osc.connect(g).connect(a.destination);
  osc.start(t);
  osc.stop(t + duration + 0.02);
}

function noise(duration: number, gain: number, filterFrom: number, filterTo: number) {
  const a = audio();
  if (!a) return;
  const t = a.currentTime;
  const buffer = a.createBuffer(1, Math.ceil(a.sampleRate * duration), a.sampleRate);
  const data = buffer.getChannelData(0);
  for (let i = 0; i < data.length; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / data.length);
  const src = a.createBufferSource();
  src.buffer = buffer;
  const filter = a.createBiquadFilter();
  filter.type = "bandpass";
  filter.frequency.setValueAtTime(filterFrom, t);
  filter.frequency.exponentialRampToValueAtTime(filterTo, t + duration);
  const g = a.createGain();
  g.gain.setValueAtTime(gain, t);
  g.gain.exponentialRampToValueAtTime(0.0001, t + duration);
  src.connect(filter).connect(g).connect(a.destination);
  src.start(t);
}

export const sfx = {
  pop: () => tone(520, 180, 0.09, "triangle", 0.12),
  tick: () => tone(900, 700, 0.04, "square", 0.03),
  whoosh: () => {
    noise(0.32, 0.22, 400, 3200);
    tone(220, 660, 0.22, "sawtooth", 0.035);
  },
  ding: () => {
    tone(1046, 1046, 0.35, "sine", 0.1);
    tone(1568, 1568, 0.45, "sine", 0.07, 0.09);
  },
  bonk: () => tone(180, 70, 0.18, "square", 0.06)
};
