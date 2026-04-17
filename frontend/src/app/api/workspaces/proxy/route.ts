import { NextResponse } from "next/server"
import { apiFetch } from "@/lib/api"

export async function POST(req: Request) {
  const body = await req.json()
  try {
    const created = await apiFetch<{ id: string }>("/workspaces", { method: "POST", body: JSON.stringify(body) })
    return NextResponse.json(created, { status: 201 })
  } catch (e: any) {
    return NextResponse.json({ error: e.message }, { status: 500 })
  }
}
