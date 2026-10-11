export type SearchParams = Record<string, string | string[] | undefined>;

export type SortDirection = "asc" | "desc";

export type ListParams = {
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: SortDirection;
};

export type QueryParams = Record<string, string | number | undefined>;

export const pageSizes = [10, 25, 50];

export const firstPage = 1;

export function param(searchParams: SearchParams, name: string): string | undefined {
  const value = searchParams[name];
  const first = Array.isArray(value) ? value[0] : value;
  const trimmed = first?.trim();

  return trimmed ? trimmed : undefined;
}

export function parseListParams(
  searchParams: SearchParams,
  sortFields: readonly string[],
): ListParams {
  const pageNumber = positiveInteger(param(searchParams, "pageNumber")) ?? firstPage;
  const pageSize = positiveInteger(param(searchParams, "pageSize"));
  const sortBy = param(searchParams, "sortBy");
  const sortDirection = param(searchParams, "sortDirection")?.toLowerCase();

  return {
    pageNumber,
    pageSize: pageSize !== undefined && pageSizes.includes(pageSize) ? pageSize : pageSizes[0],
    sortBy: sortBy !== undefined && sortFields.includes(sortBy) ? sortBy : undefined,
    sortDirection: sortDirection === "asc" || sortDirection === "desc" ? sortDirection : undefined,
  };
}

export function listQuery(list: ListParams): QueryParams {
  return {
    pageNumber: list.pageNumber === firstPage ? undefined : list.pageNumber,
    pageSize: list.pageSize === pageSizes[0] ? undefined : list.pageSize,
    sortBy: list.sortBy,
    sortDirection: list.sortDirection,
  };
}

export function hrefWith(
  pathname: string,
  current: QueryParams,
  changes: QueryParams = {},
): string {
  const query = new URLSearchParams();

  for (const [name, value] of Object.entries({ ...current, ...changes })) {
    if (value !== undefined && value !== "") query.set(name, String(value));
  }

  const text = query.toString();
  return text ? `${pathname}?${text}` : pathname;
}

export function nextSort(
  list: ListParams,
  field: string,
  defaultField: string,
  firstDirection: SortDirection,
): { sortBy: string; sortDirection: SortDirection } {
  const activeField = list.sortBy ?? defaultField;
  const activeDirection = list.sortDirection ?? "asc";

  if (activeField !== field) return { sortBy: field, sortDirection: firstDirection };

  return { sortBy: field, sortDirection: activeDirection === "asc" ? "desc" : "asc" };
}

export function activeSort(
  list: ListParams,
  field: string,
  defaultField: string,
): SortDirection | undefined {
  return (list.sortBy ?? defaultField) === field ? (list.sortDirection ?? "asc") : undefined;
}

function positiveInteger(value: string | undefined): number | undefined {
  if (value === undefined || !/^\d+$/.test(value)) return undefined;

  const number = Number(value);
  return Number.isSafeInteger(number) && number > 0 ? number : undefined;
}
