import Link from "next/link";
import {
  activeSort,
  hrefWith,
  nextSort,
  type ListParams,
  type QueryParams,
  type SortDirection,
} from "@/lib/paging";
import { headerCell } from "./styles";

export function SortHeader({
  label,
  title,
  field,
  defaultField,
  firstDirection = "asc",
  pathname,
  query,
  list,
  align = "left",
}: {
  label: string;
  title?: string;
  field: string;
  defaultField: string;
  firstDirection?: SortDirection;
  pathname: string;
  query: QueryParams;
  list: ListParams;
  align?: "left" | "right";
}) {
  const direction = activeSort(list, field, defaultField);
  const href = hrefWith(pathname, query, {
    ...nextSort(list, field, defaultField, firstDirection),
    pageNumber: undefined,
  });

  return (
    <th
      scope="col"
      className={`${headerCell} ${align === "right" ? "text-right" : ""}`}
      aria-sort={direction === "asc" ? "ascending" : direction === "desc" ? "descending" : "none"}
    >
      <Link href={href} className="inline-flex items-center gap-1 hover:text-slate-900">
        {title ? (
          <abbr title={title} className="no-underline">
            {label}
          </abbr>
        ) : (
          label
        )}
        <span aria-hidden="true" className={direction ? "text-emerald-700" : "text-slate-300"}>
          {direction === "desc" ? "▼" : "▲"}
        </span>
      </Link>
    </th>
  );
}
