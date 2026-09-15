// Many image hosts refuse requests without the reader page as Referer ("hotlink protection"). Session
// rules add the right Referer only to the extension's own requests (tabId -1), never to normal browsing.

const rules = new Map<string, { id: number; referer: string }>();
let nextId = 1000;
let initialized = false;

async function init(): Promise<void> {
  if (initialized) return;
  initialized = true;
  const existing = await chrome.declarativeNetRequest.getSessionRules();
  for (const rule of existing) {
    const host = rule.condition.requestDomains?.[0];
    const referer = rule.action.requestHeaders?.find((h) => h.header.toLowerCase() === "referer")?.value;
    if (host && referer) rules.set(host, { id: rule.id, referer });
    nextId = Math.max(nextId, rule.id + 1);
  }
}

export async function ensureReferer(url: string, referer: string): Promise<void> {
  await init();
  let host: string;
  let refererOrigin: string;
  try {
    host = new URL(url).hostname;
    refererOrigin = new URL(referer).origin;
  } catch {
    return;
  }
  const current = rules.get(host);
  if (current?.referer === referer) return;
  const id = current?.id ?? nextId++;
  rules.set(host, { id, referer });
  await chrome.declarativeNetRequest.updateSessionRules({
    removeRuleIds: [id],
    addRules: [
      {
        id,
        priority: 1,
        condition: {
          requestDomains: [host],
          tabIds: [chrome.tabs.TAB_ID_NONE],
          resourceTypes: [
            chrome.declarativeNetRequest.ResourceType.XMLHTTPREQUEST,
            chrome.declarativeNetRequest.ResourceType.IMAGE,
            chrome.declarativeNetRequest.ResourceType.OTHER,
            chrome.declarativeNetRequest.ResourceType.MEDIA
          ]
        },
        action: {
          type: chrome.declarativeNetRequest.RuleActionType.MODIFY_HEADERS,
          requestHeaders: [
            { header: "Referer", operation: chrome.declarativeNetRequest.HeaderOperation.SET, value: referer },
            { header: "Origin", operation: chrome.declarativeNetRequest.HeaderOperation.SET, value: refererOrigin }
          ]
        }
      }
    ]
  });
}
