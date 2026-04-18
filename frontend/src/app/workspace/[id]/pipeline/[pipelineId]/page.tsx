import { auth } from "@/auth"
import { redirect } from "next/navigation"
import { apiFetch } from "@/lib/api"
import type { BoardData, Workspace } from "@/types/api"
import { BoardShell } from "@/components/board/BoardShell"
import { WorkspaceTopNav } from "@/components/WorkspaceTopNav"

export default async function PipelinePage({
  params,
}: {
  params: Promise<{ id: string; pipelineId: string }>
}) {
  const { id, pipelineId } = await params
  const session = await auth()
  if (!session) redirect(`/login?callbackUrl=/workspace/${id}/pipeline/${pipelineId}`)

  const [workspaces, boardData] = await Promise.all([
    apiFetch<Workspace[]>("/workspaces"),
    apiFetch<BoardData>(`/pipelines/${pipelineId}/board`, { tenantId: id }),
  ])

  const workspace = workspaces.find((w) => w.id === id)
  if (!workspace) redirect("/workspace/select")

  return (
    <div className="flex flex-col h-screen">
      <WorkspaceTopNav workspaceId={id} workspaceName={workspace.name} />
      <div className="flex flex-1 overflow-hidden">
        <BoardShell
          initialBoard={boardData}
          workspaceId={id}
          pipelineId={pipelineId}
          workspace={workspace}
        />
      </div>
    </div>
  )
}
