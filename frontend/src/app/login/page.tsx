import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { LoginForm } from "@/components/forms/login-form";
import { card } from "@/components/styles";
import { getI18n } from "@/i18n/server";
import { getSession } from "@/lib/auth/session";
import { param } from "@/lib/paging";
import { safeReturnTo } from "@/lib/return-to";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getI18n();
  return { title: t("auth.login") };
}

export default async function LoginPage({ searchParams }: PageProps<"/login">) {
  const search = await searchParams;
  const returnTo = safeReturnTo(param(search, "returnTo"));
  const expired = param(search, "expired") === "1";

  const [{ t }, session] = await Promise.all([getI18n(), getSession()]);
  if (session) redirect(returnTo);

  return (
    <div className="mx-auto max-w-sm space-y-4 pt-6">
      <div className={`${card} space-y-4 p-6`}>
        <div>
          <h1 className="text-xl font-bold">{t("auth.title")}</h1>
          <p className="mt-1 text-sm text-slate-600">{t("auth.subtitle")}</p>
        </div>
        {expired && (
          <p
            role="status"
            className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
          >
            {t("auth.expired")}
          </p>
        )}
        <LoginForm returnTo={returnTo} />
      </div>
    </div>
  );
}
