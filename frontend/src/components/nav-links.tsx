"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { Suspense } from "react";

export function NavLinks({ links }: { links: { href: string; label: string }[] }) {
  const pathname = usePathname();

  return (
    <nav className="flex flex-wrap gap-1 text-sm">
      {links.map((link) => {
        const active = link.href === "/" ? pathname === "/" : pathname.startsWith(link.href);

        return (
          <Link
            key={link.href}
            href={link.href}
            aria-current={active ? "page" : undefined}
            className={
              active
                ? "rounded-md bg-emerald-50 px-3 py-1.5 font-medium text-emerald-800"
                : "rounded-md px-3 py-1.5 text-slate-600 hover:bg-slate-100 hover:text-slate-900"
            }
          >
            {link.label}
          </Link>
        );
      })}
    </nav>
  );
}

export function LoginLink({ label }: { label: string }) {
  return (
    <Suspense fallback={<LoginAnchor href="/login" label={label} />}>
      <LoginLinkWithReturn label={label} />
    </Suspense>
  );
}

function LoginLinkWithReturn({ label }: { label: string }) {
  const pathname = usePathname();
  const search = useSearchParams().toString();
  const current = search ? `${pathname}?${search}` : pathname;

  if (pathname === "/login") return <LoginAnchor href="/login" label={label} />;

  return <LoginAnchor href={`/login?returnTo=${encodeURIComponent(current)}`} label={label} />;
}

function LoginAnchor({ href, label }: { href: string; label: string }) {
  return (
    <Link
      href={href}
      className="rounded-md bg-slate-800 px-3 py-1.5 font-medium text-white hover:bg-slate-700"
    >
      {label}
    </Link>
  );
}
