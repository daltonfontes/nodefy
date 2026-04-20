import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function POST(req: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const body = await req.json()
  try {
    const data = await apiFetch(`/pipelines/${id}/stages`, { method: "POST", body: JSON.stringify(body) })
    return NextResponse.json(data, { status: 201 })
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
