"use client"
import { useState, useCallback } from "react"
import { useQueryClient } from "@tanstack/react-query"
import {
  DndContext,
  DragOverlay,
  closestCorners,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  DragStartEvent,
  DragEndEvent,
  DragOverEvent,
} from "@dnd-kit/core"
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable"
import { toast } from "sonner"
import { Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { KanbanColumn } from "./KanbanColumn"
import { CardDragOverlay } from "./CardDragOverlay"
import { CardDetailPanel } from "./CardDetailPanel"
import { PipelineSidebar } from "@/components/sidebar/PipelineSidebar"
import { useBoard } from "@/hooks/use-board"
import { useStages } from "@/hooks/use-stages"
import { useUIStore } from "@/store/ui-store"
import type { BoardData, CardSummary, Workspace } from "@/types/api"

interface BoardShellProps {
  initialBoard: BoardData
  workspaceId: string
  pipelineId: string
  workspace: Workspace
}

export function BoardShell({ initialBoard, workspaceId, pipelineId, workspace }: BoardShellProps) {
  const { board, moveMutation } = useBoard(pipelineId, workspaceId, initialBoard)
  const { createMutation: createStage } = useStages(pipelineId)
  const { setDraggingCardId } = useUIStore()
  const qc = useQueryClient()

  const [activeCard, setActiveCard] = useState<CardSummary | null>(null)
  const [overColumnId, setOverColumnId] = useState<string | null>(null)
  const [newStageName, setNewStageName] = useState("")
  const [addStageOpen, setAddStageOpen] = useState(false)
  const [newCardStageId, setNewCardStageId] = useState<string | null>(null)
  const [newCardTitle, setNewCardTitle] = useState("")

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  const handleDragStart = useCallback(
    (event: DragStartEvent) => {
      const card = event.active.data.current?.card as CardSummary | undefined
      if (card) {
        setActiveCard(card)
        setDraggingCardId(card.id)
      }
    },
    [setDraggingCardId]
  )

  const handleDragOver = useCallback((event: DragOverEvent) => {
    const overId = event.over?.id as string | undefined
    if (!overId) {
      setOverColumnId(null)
      return
    }
    // over.data.current?.type === "column" means we're over a column header or droppable
    const overData = event.over?.data.current
    if (overData?.type === "column") {
      setOverColumnId(overId)
    } else {
      // over is a card — find its stageId
      const stageId = overData?.stageId as string | undefined
      setOverColumnId(stageId ?? null)
    }
  }, [])

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event
      setActiveCard(null)
      setDraggingCardId(null)
      setOverColumnId(null)

      if (!over || active.id === over.id) return

      const cardId = active.id as string
      const activeData = active.data.current
      const overData = over.data.current

      const sourceStageId = activeData?.stageId as string
      // Determine target stage: if over a card, use its stageId; if over a column droppable, use the column id
      const targetStageId = (overData?.stageId ?? over.id) as string

      if (!sourceStageId || !targetStageId) return

      // Find positions for fractional indexing
      const targetStage = board.stages.find((s) => s.id === targetStageId)
      const targetCards = targetStage?.cards ?? []

      let prevPosition: number | null = null
      let nextPosition: number | null = null

      if (sourceStageId === targetStageId) {
        // Same-stage reorder: compute neighbors around the card being dropped over
        const overCardId = overData?.type === "card" ? (over.id as string) : null
        if (overCardId) {
          const overIdx = targetCards.findIndex((c) => c.id === overCardId)
          if (overIdx !== -1) {
            prevPosition = targetCards[overIdx - 1]?.position ?? null
            nextPosition = targetCards[overIdx]?.position ?? null
          }
        } else {
          // Dropped on column itself — place at end
          prevPosition = targetCards.length > 0 ? targetCards[targetCards.length - 1].position : null
          nextPosition = null
        }
      } else {
        // Cross-stage move — place at end of target column
        prevPosition = targetCards.length > 0 ? targetCards[targetCards.length - 1].position : null
        nextPosition = null
      }

      moveMutation.mutate(
        { cardId, targetStageId, prevPosition, nextPosition },
        {
          onError: () => {
            toast.error("Não foi possível mover o card. Tente novamente.")
          },
        }
      )
    },
    [board, moveMutation, setDraggingCardId]
  )

  function handleCreateStage() {
    const name = newStageName.trim()
    if (!name) return
    createStage.mutate(name)
    setNewStageName("")
    setAddStageOpen(false)
  }

  async function handleCreateCard(stageId: string) {
    const title = newCardTitle.trim()
    if (!title) return
    await fetch(`/api/pipelines/${pipelineId}/cards`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title, stageId }),
    })
    setNewCardTitle("")
    setNewCardStageId(null)
    qc.invalidateQueries({ queryKey: ["board", pipelineId] })
  }

  return (
    <div className="flex h-full overflow-hidden">
      <PipelineSidebar
        workspaceId={workspaceId}
        activePipelineId={pipelineId}
        role={workspace.role}
      />

      <main className="flex flex-col flex-1 overflow-hidden">
        {/* Board header */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-border bg-background">
          <h2 className="text-sm font-semibold">{board.pipeline.name}</h2>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setNewCardStageId(board.stages[0]?.id ?? null)}
            disabled={board.stages.length === 0}
          >
            <Plus className="h-4 w-4 mr-1" />
            Criar card
          </Button>
        </div>

        {/* New card form */}
        {newCardStageId && (
          <div className="flex items-center gap-2 px-4 py-2 border-b border-border bg-muted/30">
            <Input
              value={newCardTitle}
              onChange={(e) => setNewCardTitle(e.target.value)}
              placeholder="Título do card..."
              className="max-w-sm"
              autoFocus
              onKeyDown={(e) => {
                if (e.key === "Enter") handleCreateCard(newCardStageId)
                if (e.key === "Escape") {
                  setNewCardTitle("")
                  setNewCardStageId(null)
                }
              }}
            />
            <Button size="sm" onClick={() => handleCreateCard(newCardStageId)}>
              Criar
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                setNewCardTitle("")
                setNewCardStageId(null)
              }}
            >
              Cancelar
            </Button>
          </div>
        )}

        {/* Kanban board */}
        <div className="flex-1 overflow-x-auto overflow-y-hidden">
          <div className="flex gap-4 p-4 h-full items-start">
            {board.stages.length === 0 ? (
              <div className="flex flex-col items-center justify-center w-full h-full gap-4 text-muted-foreground">
                <p className="text-lg font-medium">Nenhum estágio ainda</p>
                <Button
                  variant="outline"
                  onClick={() => setAddStageOpen(true)}
                >
                  <Plus className="h-4 w-4 mr-1" />
                  Adicionar estágio
                </Button>
              </div>
            ) : (
              <DndContext
                sensors={sensors}
                collisionDetection={closestCorners}
                onDragStart={handleDragStart}
                onDragOver={handleDragOver}
                onDragEnd={handleDragEnd}
              >
                {board.stages.map((stage) => (
                  <KanbanColumn
                    key={stage.id}
                    stage={stage}
                    pipelineId={pipelineId}
                    workspaceId={workspaceId}
                    workspaceCurrency={workspace.currency}
                    role={workspace.role}
                    isOver={overColumnId === stage.id}
                  />
                ))}

                <DragOverlay>
                  {activeCard ? (
                    <CardDragOverlay card={activeCard} workspaceCurrency={workspace.currency} />
                  ) : null}
                </DragOverlay>
              </DndContext>
            )}

            {/* Add stage */}
            <Popover open={addStageOpen} onOpenChange={setAddStageOpen}>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className="flex-shrink-0 h-10"
                  onClick={() => setAddStageOpen(true)}
                >
                  <Plus className="h-4 w-4 mr-1" />
                  Adicionar estágio
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-64">
                <div className="flex flex-col gap-3">
                  <p className="text-sm font-medium">Novo estágio</p>
                  <Input
                    value={newStageName}
                    onChange={(e) => setNewStageName(e.target.value)}
                    placeholder="Nome do estágio"
                    autoFocus
                    onKeyDown={(e) => {
                      if (e.key === "Enter") handleCreateStage()
                      if (e.key === "Escape") setAddStageOpen(false)
                    }}
                  />
                  <div className="flex gap-2">
                    <Button size="sm" className="flex-1" onClick={handleCreateStage}>
                      Criar
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1"
                      onClick={() => setAddStageOpen(false)}
                    >
                      Cancelar
                    </Button>
                  </div>
                </div>
              </PopoverContent>
            </Popover>
          </div>
        </div>
      </main>

      <CardDetailPanel workspaceId={workspaceId} pipelineId={pipelineId} workspaceCurrency={workspace.currency} />
    </div>
  )
}
