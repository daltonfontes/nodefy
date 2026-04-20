import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const workspaceId = new URL(req.url).searchParams.get("workspaceId") ?? undefined
  try {
    const data = await apiFetch(`/cards/${id}`, { tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}

export async function PATCH(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const workspaceId = new URL(req.url).searchParams.get("workspaceId") ?? undefined
  const body = await req.json()
  try {
    const data = await apiFetch(`/cards/${id}`, { method: "PATCH", body: JSON.stringify(body), tenantId: workspaceId })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
