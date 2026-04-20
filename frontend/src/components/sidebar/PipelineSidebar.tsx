"use client"
import { useState } from "react"
import { ChevronLeft, ChevronRight, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { PipelineListItem } from "./PipelineListItem"
import { usePipelines } from "@/hooks/use-pipelines"
import { useUIStore } from "@/store/ui-store"

interface PipelineSidebarProps {
  workspaceId: string
  activePipelineId: string
  role: "admin" | "member"
}

export function PipelineSidebar({ workspaceId, activePipelineId, role }: PipelineSidebarProps) {
  const { sidebarCollapsed, setSidebarCollapsed } = useUIStore()
  const { pipelines, createMutation } = usePipelines(workspaceId)
  const [newPipelineName, setNewPipelineName] = useState("")
  const [createOpen, setCreateOpen] = useState(false)

  function handleCreatePipeline() {
    const name = newPipelineName.trim()
    if (!name) return
    createMutation.mutate(name)
    setNewPipelineName("")
    setCreateOpen(false)
  }

  return (
    <aside
      className={`bg-muted flex flex-col h-full transition-all duration-200 ease-in-out border-r border-border flex-shrink-0 ${
        sidebarCollapsed ? "w-12" : "w-60"
      }`}
    >
      {/* Toggle button */}
      <div className="flex items-center justify-end p-2">
        <button
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          aria-label={sidebarCollapsed ? "Expandir barra lateral" : "Recolher barra lateral"}
          className="h-8 w-8 flex items-center justify-center rounded hover:bg-accent transition-colors"
        >
          {sidebarCollapsed ? (
            <ChevronRight className="h-4 w-4" />
          ) : (
            <ChevronLeft className="h-4 w-4" />
          )}
        </button>
      </div>

      {/* Section label */}
      {!sidebarCollapsed && (
        <div className="px-3 mb-2">
          <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            PIPELINES
          </span>
        </div>
      )}

      {/* Pipeline list */}
      <div className="flex-1 overflow-y-auto">
        {pipelines.length === 0 && !sidebarCollapsed ? (
          <div className="px-3 py-4 text-center">
            <p className="text-sm text-muted-foreground mb-2">Nenhum pipeline ainda</p>
            {role === "admin" && (
              <button
                className="text-sm text-primary hover:underline"
                onClick={() => setCreateOpen(true)}
              >
                ＋ Criar pipeline
              </button>
            )}
          </div>
        ) : (
          <div className="flex flex-col gap-0.5 px-1">
            {pipelines.map((pipeline) => (
              <PipelineListItem
                key={pipeline.id}
                pipeline={pipeline}
                isActive={pipeline.id === activePipelineId}
                workspaceId={workspaceId}
                role={role}
                collapsed={sidebarCollapsed}
              />
            ))}
          </div>
        )}
      </div>

      {/* Footer: New pipeline */}
      {role === "admin" && (
        <div className="p-2 border-t border-border">
          {sidebarCollapsed ? (
            <button
              onClick={() => setSidebarCollapsed(false)}
              aria-label="Novo pipeline"
              className="h-8 w-8 flex items-center justify-center rounded hover:bg-accent transition-colors mx-auto"
            >
              <Plus className="h-4 w-4" />
            </button>
          ) : (
            <Popover open={createOpen} onOpenChange={setCreateOpen}>
              <PopoverTrigger asChild>
                <Button variant="ghost" className="w-full justify-start text-sm" size="sm">
                  <Plus className="h-4 w-4 mr-1" />
                  Novo pipeline
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-64" side="right">
                <div className="flex flex-col gap-3">
                  <p className="text-sm font-medium">Novo pipeline</p>
                  <Input
                    value={newPipelineName}
                    onChange={(e) => setNewPipelineName(e.target.value)}
                    placeholder="Nome do pipeline"
                    autoFocus
                    onKeyDown={(e) => {
                      if (e.key === "Enter") handleCreatePipeline()
                      if (e.key === "Escape") setCreateOpen(false)
                    }}
                  />
                  <div className="flex gap-2">
                    <Button size="sm" className="flex-1" onClick={handleCreatePipeline}>
                      Criar
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1"
                      onClick={() => setCreateOpen(false)}
                    >
                      Cancelar
                    </Button>
                  </div>
                </div>
              </PopoverContent>
            </Popover>
          )}
        </div>
      )}
    </aside>
  )
}
