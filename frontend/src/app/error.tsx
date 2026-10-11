"use client";

import { card, primaryButton } from "@/components/styles";
import { useI18n } from "@/i18n/client";

export default function ErrorPage({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  retry: () => void;
}) {
  const { t } = useI18n();

  return (
    <div role="alert" className={`${card} mx-auto max-w-md space-y-3 p-6 text-center`}>
      <h1 className="text-xl font-bold">{t("errorPage.title")}</h1>
      <p className="text-sm text-slate-600">{t("errorPage.description")}</p>
      {error.digest && (
        <p className="text-xs text-slate-500">
          {t("errorPage.digest")} <code className="select-all font-mono">{error.digest}</code>
        </p>
      )}
      <button type="button" onClick={() => retry()} className={primaryButton}>
        {t("errorPage.retry")}
      </button>
    </div>
  );
}
