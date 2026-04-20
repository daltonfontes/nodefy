import { auth } from "@/auth"
import { mintApiToken } from "./api-token"

const baseUrl =
  process.env.INTERNAL_API_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000"

export async function apiFetch<T>(path: string, init?: RequestInit & { tenantId?: string }): Promise<T> {
  const session = await auth()
  if (!session?.user) throw new Error("Not authenticated")
  const token = await mintApiToken({
    sub: (session as any).sub ?? session.user.id ?? "",
    email: session.user.email ?? "",
    tenant_id: init?.tenantId,
  })
  const res = await fetch(`${baseUrl}${path}`, {
    ...init,
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...(init?.tenantId ? { "X-Tenant-Id": init.tenantId } : {}),
      ...init?.headers,
    },
  })
  if (!res.ok) {
    const text = await res.text().catch(() => "")
    throw new ApiError(res.status, text)
  }
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

export class ApiError extends Error {
  constructor(public status: number, public body: string) { super(`API ${status}: ${body}`) }
}
