// When the extension is reloaded or updated, scripts already running in open tabs lose their link to it.
// From then on every chrome.runtime call throws "Extension context invalidated" synchronously (a
// .catch() never sees it), so content scripts check first and quietly stand down instead.

export function extensionAlive(): boolean {
  try {
    return !!chrome.runtime?.id;
  } catch {
    return false;
  }
}

/** sendMessage that never throws: resolves undefined when the extension is gone or doesn't answer. */
export function sendSafely<T = unknown>(message: unknown): Promise<T | undefined> {
  if (!extensionAlive()) return Promise.resolve(undefined);
  try {
    return (chrome.runtime.sendMessage(message) as Promise<T>).catch(() => undefined);
  } catch {
    return Promise.resolve(undefined);
  }
}
