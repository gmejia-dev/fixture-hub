import Link from "next/link";
import { card, primaryButton } from "@/components/styles";
import { getI18n } from "@/i18n/server";

export default async function NotFound() {
  const { t } = await getI18n();

  return (
    <div className={`${card} mx-auto max-w-md space-y-3 p-6 text-center`}>
      <h1 className="text-xl font-bold">{t("notFound.title")}</h1>
      <p className="text-sm text-slate-600">{t("notFound.description")}</p>
      <Link href="/" className={primaryButton}>
        {t("notFound.home")}
      </Link>
    </div>
  );
}
