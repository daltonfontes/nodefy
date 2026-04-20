import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function PATCH(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const workspaceId = new URL(req.url).searchParams.get("workspaceId") ?? undefined
  try {
    const data = await apiFetch(`/cards/${id}/archive`, { method: "PATCH", body: "{}", tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
