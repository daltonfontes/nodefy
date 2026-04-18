import { NextResponse } from "next/server"
import { apiFetch, ApiError } from "@/lib/api"

export async function PATCH(req: Request, { params }: { params: Promise<{ id: string; userId: string }> }) {
  const { id, userId } = await params
  const body = await req.json()
  try {
    const data = await apiFetch(`/workspaces/${id}/members/${userId}`, { method: "PATCH", body: JSON.stringify(body), tenantId: id })
    return NextResponse.json(data)
  } catch (e: any) {
    const status = e instanceof ApiError ? e.status : 500
    return NextResponse.json({ error: e.message }, { status })
  }
}

export async function DELETE(_req: Request, { params }: { params: Promise<{ id: string; userId: string }> }) {
  const { id, userId } = await params
  try {
    await apiFetch(`/workspaces/${id}/members/${userId}`, { method: "DELETE", tenantId: id })
    return new NextResponse(null, { status: 204 })
  } catch (e: any) {
    const status = e instanceof ApiError ? e.status : 500
    return NextResponse.json({ error: e.message }, { status })
  }
}
