// Connection limits: a global cap plus an adaptive per-host cap that backs off when a host rate-limits.

interface HostState {
  active: number;
  limit: number;
  queue: Array<() => void>;
  cooldownUntil: number;
}

export class ConnectionLimiter {
  private globalActive = 0;
  private readonly hosts = new Map<string, HostState>();
  private readonly waiting: Array<{ host: string; resume: () => void }> = [];

  constructor(private perHost: number, private global: number) {}

  configure(perHost: number, global: number): void {
    this.perHost = perHost;
    this.global = global;
    for (const state of this.hosts.values()) state.limit = Math.min(state.limit, perHost) || perHost;
    this.pump();
  }

  private state(host: string): HostState {
    let s = this.hosts.get(host);
    if (!s) {
      s = { active: 0, limit: this.perHost, queue: [], cooldownUntil: 0 };
      this.hosts.set(host, s);
    }
    return s;
  }

  async acquire(host: string, signal?: AbortSignal): Promise<() => void> {
    if (signal?.aborted) throw new DOMException("Aborted", "AbortError");
    await new Promise<void>((resolve, reject) => {
      const entry = { host, resume: resolve };
      this.waiting.push(entry);
      signal?.addEventListener("abort", () => {
        const i = this.waiting.indexOf(entry);
        if (i >= 0) this.waiting.splice(i, 1);
        reject(new DOMException("Aborted", "AbortError"));
      }, { once: true });
      this.pump();
    });
    let released = false;
    return () => {
      if (released) return;
      released = true;
      this.globalActive--;
      this.state(host).active--;
      this.pump();
    };
  }

  /** A host answered 429/503: halve its connections for a while. */
  throttle(host: string, retryAfterMs: number): void {
    const s = this.state(host);
    s.limit = Math.max(1, Math.floor(s.limit / 2));
    s.cooldownUntil = Date.now() + Math.min(60_000, Math.max(1000, retryAfterMs));
    setTimeout(() => this.pump(), retryAfterMs + 20);
  }

  /** Successful responses slowly restore a throttled host. */
  recover(host: string): void {
    const s = this.state(host);
    if (s.limit < this.perHost && Date.now() > s.cooldownUntil) s.limit++;
  }

  private pump(): void {
    const now = Date.now();
    for (let i = 0; i < this.waiting.length && this.globalActive < this.global; ) {
      const { host, resume } = this.waiting[i];
      const s = this.state(host);
      if (s.active < s.limit && now >= s.cooldownUntil) {
        this.waiting.splice(i, 1);
        s.active++;
        this.globalActive++;
        resume();
      } else {
        i++;
      }
    }
  }
}

/** Bytes per second over a sliding window. */
export class SpeedMeter {
  private samples: Array<{ t: number; bytes: number }> = [];

  add(bytes: number): void {
    const now = Date.now();
    this.samples.push({ t: now, bytes });
    this.trim(now);
  }

  get bps(): number {
    const now = Date.now();
    this.trim(now);
    if (!this.samples.length) return 0;
    const total = this.samples.reduce((sum, s) => sum + s.bytes, 0);
    const span = Math.max(1000, now - this.samples[0].t);
    return (total / span) * 1000;
  }

  private trim(now: number): void {
    while (this.samples.length && now - this.samples[0].t > 4000) this.samples.shift();
  }
}
