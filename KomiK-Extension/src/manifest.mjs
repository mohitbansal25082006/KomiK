// Manifest V3 for Chrome, Edge and Brave. Version 1.0.0 pairs with the Komik 1.2.0 desktop app,
// which reads the ComicInfo.xml metadata and tags this extension embeds.
export const EXTENSION_VERSION = "1.0.0";

export function buildManifest() {
  return {
    manifest_version: 3,
    name: "KomiK Downloader",
    short_name: "KomiK",
    version: EXTENSION_VERSION,
    description:
      "Download comics, manga and webtoons from any website as CBZ with titles, credits and tags embedded for the Komik reader.",
    minimum_chrome_version: "116",
    icons: {
      16: "icons/icon-16.png",
      32: "icons/icon-32.png",
      48: "icons/icon-48.png",
      128: "icons/icon-128.png"
    },
    action: {
      default_title: "KomiK Downloader",
      default_popup: "popup.html",
      default_icon: {
        16: "icons/icon-16.png",
        32: "icons/icon-32.png",
        48: "icons/icon-48.png"
      }
    },
    side_panel: { default_path: "sidepanel.html" },
    options_ui: { page: "options.html", open_in_tab: true },
    background: { service_worker: "background.js" },
    content_scripts: [
      {
        matches: ["http://*/*", "https://*/*"],
        js: ["content/sentinel.js"],
        run_at: "document_idle",
        all_frames: false
      }
    ],
    permissions: [
      "downloads",
      "downloads.open",
      "storage",
      "unlimitedStorage",
      "scripting",
      "tabs",
      "contextMenus",
      "notifications",
      "offscreen",
      "sidePanel",
      "declarativeNetRequestWithHostAccess",
      "webRequest",
      "alarms"
    ],
    host_permissions: ["<all_urls>"],
    commands: {
      _execute_action: {
        suggested_key: { default: "Alt+K" },
        description: "Open KomiK Downloader"
      },
      "quick-download": {
        suggested_key: { default: "Alt+Shift+K" },
        description: "Download the comic on this page with your default settings"
      },
      "open-side-panel": {
        description: "Open the download manager side panel"
      }
    },
    web_accessible_resources: [
      {
        resources: ["fonts/*"],
        matches: ["http://*/*", "https://*/*"],
        use_dynamic_url: true
      }
    ]
  };
}
