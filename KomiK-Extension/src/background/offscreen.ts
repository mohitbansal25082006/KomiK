// The download engine runs in an offscreen document: it can build Blobs and blob: URLs, decode and
// convert images, write to a custom folder, and keep working while the service worker sleeps.
import { envelope, type MessageMap, type MessageType } from "@/shared/messages";

const OFFSCREEN_URL = "offscreen.html";
let creating: Promise<void> | null = null;

async function hasOffscreen(): Promise<boolean> {
  const contexts = await chrome.runtime.getContexts({
    contextTypes: [chrome.runtime.ContextType.OFFSCREEN_DOCUMENT],
    documentUrls: [chrome.runtime.getURL(OFFSCREEN_URL)]
  });
  return contexts.length > 0;
}

export async function ensureOffscreen(): Promise<void> {
  if (await hasOffscreen()) return;
  if (!creating) {
    creating = chrome.offscreen
      .createDocument({
        url: OFFSCREEN_URL,
        reasons: [chrome.offscreen.Reason.BLOBS, chrome.offscreen.Reason.WORKERS],
        justification: "Builds CBZ/PDF files from downloaded comic pages and hands them to the downloads API."
      })
      .catch((err: unknown) => {
        if (!String(err).includes("Only a single offscreen")) throw err;
      })
      .finally(() => {
        creating = null;
      });
  }
  await creating;
}

export async function toEngine<T extends MessageType>(type: T, payload: MessageMap[T]["req"]): Promise<MessageMap[T]["res"]> {
  await ensureOffscreen();
  // The engine registers its listener right after load; retry briefly while it boots.
  for (let attempt = 0; attempt < 20; attempt++) {
    try {
      const res = await chrome.runtime.sendMessage(envelope(type, payload, "offscreen"));
      if (res !== undefined) return res as MessageMap[T]["res"];
    } catch {
      /* receiving end not ready yet */
    }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error("The download engine did not start.");
}
