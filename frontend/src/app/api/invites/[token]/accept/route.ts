import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function POST(_req: Request, { params }: { params: Promise<{ token: string }> }) {
  const { token } = await params
  try {
    const result = await apiFetch<{ workspaceId: string }>(`/invites/${token}/accept`, { method: "POST" })
    return NextResponse.redirect(new URL(`/workspace/${result.workspaceId}`, _req.url))
  } catch {
    return NextResponse.redirect(new URL(`/invite/${token}?error=accept_failed`, _req.url))
  }
}
