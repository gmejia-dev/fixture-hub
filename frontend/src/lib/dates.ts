type ZonedParts = {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  second: number;
};

const dateInput = /^(\d{4})-(\d{2})-(\d{2})$/;

const dateTimeInput = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

export function formatDateTime(iso: string, locale: string, timeZone: string): string {
  return new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone,
  }).format(new Date(iso));
}

export function toDateTimeInput(iso: string, timeZone: string): string {
  const p = zonedParts(new Date(iso), timeZone);
  return `${p.year}-${pad(p.month)}-${pad(p.day)}T${pad(p.hour)}:${pad(p.minute)}`;
}

export function dateTimeInputToIso(value: string, timeZone: string): string | null {
  const match = dateTimeInput.exec(value);
  if (!match) return null;

  const [year, month, day, hour, minute] = match.slice(1).map(Number);
  const wallClock = Date.UTC(year, month - 1, day, hour, minute);
  if (!sameWallClock(wallClock, { year, month, day, hour, minute })) return null;

  const guess = wallClock - offset(wallClock, timeZone);
  return new Date(wallClock - offset(guess, timeZone)).toISOString();
}

export function isDateInput(value: string | undefined): value is string {
  if (!value) return false;
  const match = dateInput.exec(value);
  if (!match) return false;

  const [year, month, day] = match.slice(1).map(Number);
  return sameWallClock(Date.UTC(year, month - 1, day), { year, month, day, hour: 0, minute: 0 });
}

export function startOfDayIso(date: string, timeZone: string): string | null {
  return dateTimeInputToIso(`${date}T00:00`, timeZone);
}

export function endOfDayIso(date: string, timeZone: string): string | null {
  const lastMinute = dateTimeInputToIso(`${date}T23:59`, timeZone);
  return lastMinute ? new Date(Date.parse(lastMinute) + 59_999).toISOString() : null;
}

function offset(utc: number, timeZone: string): number {
  const p = zonedParts(new Date(utc), timeZone);
  const wallClock = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
  return wallClock - Math.floor(utc / 1000) * 1000;
}

function zonedParts(date: Date, timeZone: string): ZonedParts {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).formatToParts(date);

  const part = (type: Intl.DateTimeFormatPartTypes) =>
    Number(parts.find((p) => p.type === type)?.value);

  return {
    year: part("year"),
    month: part("month"),
    day: part("day"),
    hour: part("hour"),
    minute: part("minute"),
    second: part("second"),
  };
}

function sameWallClock(utc: number, expected: Omit<ZonedParts, "second">): boolean {
  const date = new Date(utc);
  return (
    date.getUTCFullYear() === expected.year &&
    date.getUTCMonth() + 1 === expected.month &&
    date.getUTCDate() === expected.day &&
    date.getUTCHours() === expected.hour &&
    date.getUTCMinutes() === expected.minute
  );
}

function pad(value: number): string {
  return String(value).padStart(2, "0");
}
