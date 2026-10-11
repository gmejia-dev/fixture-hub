import Link from "next/link";
import { changeLocale } from "@/actions/locale";
import { locales } from "@/i18n/config";
import { getI18n } from "@/i18n/server";
import { NavLinks } from "./nav-links";

export async function SiteHeader() {
  const { t, locale } = await getI18n();

  const links = [
    { href: "/", label: t("nav.home") },
    { href: "/standings", label: t("nav.standings") },
    { href: "/top-scorers", label: t("nav.topScorers") },
    { href: "/matches", label: t("nav.matches") },
    { href: "/teams", label: t("nav.teams") },
  ];

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
        <Link href="/" className="flex items-center gap-2 text-lg font-bold text-slate-900">
          <svg viewBox="0 0 32 32" className="h-7 w-7" aria-hidden="true">
            <circle cx="16" cy="16" r="15" fill="#059669" />
            <circle cx="16" cy="16" r="10" fill="#ffffff" />
            <path d="M16 9.5l4.5 3.3-1.7 5.2h-5.6l-1.7-5.2z" fill="#0f172a" />
          </svg>
          FixtureHub
        </Link>

        <NavLinks links={links} />

        <div className="ml-auto flex items-center gap-3 text-sm">
          <form
            action={changeLocale}
            className="flex overflow-hidden rounded-md border border-slate-300"
          >
            {locales.map((option) => (
              <button
                key={option}
                name="locale"
                value={option}
                aria-pressed={option === locale}
                aria-label={t(`locale.${option}`)}
                className={
                  option === locale
                    ? "bg-slate-800 px-2 py-1 text-xs font-semibold text-white"
                    : "px-2 py-1 text-xs font-semibold text-slate-600 hover:bg-slate-100"
                }
              >
                {option.toUpperCase()}
              </button>
            ))}
          </form>
        </div>
      </div>
    </header>
  );
}
