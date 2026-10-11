export function GET(): Response {
  return new Response("Healthy", { headers: { "Cache-Control": "no-store" } });
}
