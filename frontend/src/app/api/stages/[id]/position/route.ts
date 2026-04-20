import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function PATCH(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const body = await req.json()
  try {
    const data = await apiFetch(`/stages/${id}/position`, { method: "PATCH", body: JSON.stringify(body) })
    return NextResponse.json(data)
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
