import Link from "next/link";
import type { ReactNode } from "react";
import { getI18n } from "@/i18n/server";
import type { PagedResult } from "@/lib/api/types";
import { firstPage, hrefWith, pageSizes, type QueryParams } from "@/lib/paging";

export async function Pagination({
  pathname,
  query,
  page,
}: {
  pathname: string;
  query: QueryParams;
  page: PagedResult<unknown>;
}) {
  const { t } = await getI18n();

  const from = page.totalRecords === 0 ? 0 : (page.pageNumber - 1) * page.pageSize + 1;
  const to = Math.min(page.pageNumber * page.pageSize, page.totalRecords);
  const pageHref = (pageNumber: number) =>
    hrefWith(pathname, query, { pageNumber: pageNumber === firstPage ? undefined : pageNumber });

  return (
    <nav
      aria-label={t("paging.label")}
      className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 px-3 py-3 text-sm text-slate-600"
    >
      <p>{t("paging.summary", { from, to, total: page.totalRecords })}</p>

      <div className="flex items-center gap-1">
        <PageLink href={page.pageNumber > firstPage ? pageHref(page.pageNumber - 1) : undefined}>
          {t("paging.previous")}
        </PageLink>
        <span className="px-2">
          {t("paging.page", { page: page.pageNumber, pages: Math.max(page.totalPages, 1) })}
        </span>
        <PageLink
          href={page.pageNumber < page.totalPages ? pageHref(page.pageNumber + 1) : undefined}
        >
          {t("paging.next")}
        </PageLink>
      </div>

      <div className="flex items-center gap-1">
        <span>{t("paging.pageSize")}</span>
        {pageSizes.map((size) => (
          <Link
            key={size}
            href={hrefWith(pathname, query, {
              pageNumber: undefined,
              pageSize: size === pageSizes[0] ? undefined : size,
            })}
            aria-current={size === page.pageSize ? "true" : undefined}
            className={
              size === page.pageSize
                ? "rounded bg-slate-800 px-2 py-0.5 text-white"
                : "rounded px-2 py-0.5 hover:bg-slate-100"
            }
          >
            {size}
          </Link>
        ))}
      </div>
    </nav>
  );
}

function PageLink({ href, children }: { href?: string; children: ReactNode }) {
  if (!href) {
    return <span className="rounded-md px-3 py-1 text-slate-300">{children}</span>;
  }

  return (
    <Link href={href} className="rounded-md border border-slate-300 px-3 py-1 hover:bg-slate-50">
      {children}
    </Link>
  );
}
