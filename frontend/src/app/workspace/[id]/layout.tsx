import { auth } from "@/auth"
import { redirect } from "next/navigation"
import { apiFetch } from "@/lib/api"
import type { Workspace } from "@/types/api"
import { WorkspaceTopNav } from "@/components/WorkspaceTopNav"

export default async function WorkspaceLayout({ children, params }: { children: React.ReactNode; params: Promise<{ id: string }> }) {
  const { id } = await params
  const session = await auth()
  if (!session) redirect(`/login?callbackUrl=/workspace/${id}`)
  const workspaces = await apiFetch<Workspace[]>("/workspaces")
  const ws = workspaces.find((w) => w.id === id)
  if (!ws) redirect("/workspace/select")
  return (
    <div className="min-h-screen bg-secondary">
      <WorkspaceTopNav workspaceId={id} workspaceName={ws.name} />
      <main className="mx-auto max-w-6xl p-6">{children}</main>
    </div>
  )
}
