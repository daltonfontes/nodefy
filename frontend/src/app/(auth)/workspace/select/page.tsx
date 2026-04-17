import { auth } from "@/auth"
import { redirect } from "next/navigation"
import Link from "next/link"
import { apiFetch } from "@/lib/api"
import type { Workspace } from "@/types/api"
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export default async function WorkspaceSelectPage() {
  const session = await auth()
  if (!session) redirect("/login")
  const workspaces = await apiFetch<Workspace[]>("/workspaces")
  if (workspaces.length === 0) redirect("/workspace/new")           // D-09
  if (workspaces.length === 1) redirect(`/workspace/${workspaces[0].id}`) // fast path
  // D-08: card grid selector
  return (
    <div className="min-h-screen bg-secondary p-8">
      <div className="mx-auto max-w-3xl space-y-6">
        <h1 className="text-2xl font-semibold">Selecione um workspace</h1>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {workspaces.map((w) => (
            <Link key={w.id} href={`/workspace/${w.id}`}>
              <Card className="hover:shadow-md transition cursor-pointer">
                <CardHeader>
                  <CardTitle className="text-lg">{w.name}</CardTitle>
                </CardHeader>
                <CardContent>
                  <Badge variant={w.role === "admin" ? "default" : "secondary"}>{w.role === "admin" ? "Admin" : "Membro"}</Badge>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
        <Link href="/workspace/new" className="text-sm text-primary underline">Criar novo workspace</Link>
      </div>
    </div>
  )
}
