import { NextResponse } from "next/server"
import { apiFetch, ApiError } from "@/lib/api"

export async function POST(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const body = await req.json()
  try {
    const data = await apiFetch(`/workspaces/${id}/invites`, { method: "POST", body: JSON.stringify(body), tenantId: id })
    return NextResponse.json(data, { status: 201 })
  } catch (e: any) {
    const status = e instanceof ApiError ? e.status : 500
    return NextResponse.json({ error: e.message }, { status })
  }
}
