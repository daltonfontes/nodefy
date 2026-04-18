import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function GET(_req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  try {
    const data = await apiFetch(`/workspaces/${id}/members`, { tenantId: id })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
