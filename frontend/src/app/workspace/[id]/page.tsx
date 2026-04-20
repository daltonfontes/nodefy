import { redirect } from "next/navigation"
import { apiFetch } from "@/lib/api"
import type { Pipeline, Workspace } from "@/types/api"
import { Card, CardContent } from "@/components/ui/card"
import { FirstPipelineForm } from "@/components/FirstPipelineForm"

export default async function WorkspaceHomePage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params

  const [pipelines, workspaces] = await Promise.all([
    apiFetch<Pipeline[]>(`/workspaces/${id}/pipelines`, { tenantId: id }),
    apiFetch<Workspace[]>("/workspaces"),
  ])

  const workspace = workspaces.find((w) => w.id === id)
  if (!workspace) redirect("/workspace/select")

  if (pipelines.length > 0) {
    const first = [...pipelines].sort((a, b) => a.position - b.position)[0]
    redirect(`/workspace/${id}/pipeline/${first.id}`)
  }

  const isAdmin = workspace.role === "admin"

  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
        {isAdmin ? (
          <>
            <h2 className="text-xl font-semibold">Comece criando seu primeiro pipeline</h2>
            <p className="text-sm text-muted-foreground">
              Pipelines organizam seus deals em estágios. Dê um nome para começar.
            </p>
            <div className="w-full max-w-sm">
              <FirstPipelineForm workspaceId={id} />
            </div>
          </>
        ) : (
          <>
            <h2 className="text-xl font-semibold">Nenhum pipeline ainda</h2>
            <p className="text-sm text-muted-foreground">
              Peça ao administrador deste workspace para criar o primeiro pipeline.
            </p>
          </>
        )}
      </CardContent>
    </Card>
  )
}
