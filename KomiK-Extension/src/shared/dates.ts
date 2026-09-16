// Release dates sit right next to chapter names on comic sites ("Chapter 1" above "12/04/2025"). When the
// text is read together it becomes "Chapter 112/04/2025", so dates are removed before any number is read.

const MONTHS = "jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?";

export const DATE_PATTERN = [
  String.raw`\b\d{1,4}[/.\-]\d{1,2}[/.\-]\d{1,4}\b`,
  String.raw`\b(?:${MONTHS})\.?\s+\d{1,2}(?:st|nd|rd|th)?,?\s*\d{2,4}\b`,
  String.raw`\b\d{1,2}(?:st|nd|rd|th)?\s+(?:${MONTHS})\.?,?\s*\d{2,4}\b`,
  String.raw`\b\d+\s*(?:second|sec|minute|min|hour|hr|day|week|month|year)s?\s*ago\b`,
  String.raw`\b(?:yesterday|today|just now)\b`,
  String.raw`\b\d{1,2}:\d{2}(?:\s*[ap]\.?m\.?)?\b`
].join("|");

/** Removes dates and "2 days ago" stamps from a piece of text. */
export function stripDates(text: string): string {
  return (text ?? "").replace(new RegExp(DATE_PATTERN, "gi"), " ").replace(/\s+/g, " ").trim();
}

/** The first date found in a piece of text, or undefined. */
export function findDate(text: string): string | undefined {
  return new RegExp(DATE_PATTERN, "i").exec(text ?? "")?.[0];
}
