import type { Metadata } from "next";
import { SiteHeader } from "@/components/site-header";
import { I18nProvider } from "@/i18n/client";
import { getI18n } from "@/i18n/server";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();

  return {
    title: { default: "FixtureHub", template: "%s · FixtureHub" },
    description: t("app.description"),
  };
}

export default async function RootLayout({ children }: LayoutProps<"/">) {
  const { locale, messages, timeZone, t } = await getI18n();

  return (
    <html lang={locale} className="h-full antialiased">
      <body className="flex min-h-full flex-col bg-slate-50 text-slate-900">
        <I18nProvider locale={locale} messages={messages} timeZone={timeZone}>
          <SiteHeader />
          <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</main>
          <footer className="border-t border-slate-200 py-4 text-center text-xs text-slate-500">
            {t("app.footer", { timeZone })}
          </footer>
        </I18nProvider>
      </body>
    </html>
  );
}
