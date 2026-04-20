import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const url = new URL(req.url)
  const workspaceId = url.searchParams.get("workspaceId")
  if (!workspaceId) {
    return NextResponse.json({ error: "workspaceId is required" }, { status: 400 })
  }
  try {
    const data = await apiFetch(`/pipelines/${id}/board`, { tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
