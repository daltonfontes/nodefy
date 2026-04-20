"use client"
import { useState, useRef } from "react"
import { SortableContext, verticalListSortingStrategy, useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { useDroppable } from "@dnd-kit/core"
import { MoreHorizontal, LayoutList } from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { KanbanCard } from "./KanbanCard"
import { useStages } from "@/hooks/use-stages"
import type { Stage, CardSummary } from "@/types/api"

interface KanbanColumnProps {
  stage: Stage & { cards: CardSummary[] }
  pipelineId: string
  workspaceId: string
  workspaceCurrency: "BRL" | "USD" | "EUR"
  role: "admin" | "member"
  isOver: boolean
}

function formatCurrency(value: number, currency: "BRL" | "USD" | "EUR") {
  const localeMap = { BRL: "pt-BR", USD: "en-US", EUR: "de-DE" }
  return new Intl.NumberFormat(localeMap[currency], {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(value)
}

export function KanbanColumn({
  stage,
  pipelineId,
  workspaceId,
  workspaceCurrency,
  role,
  isOver,
}: KanbanColumnProps) {
  const { renameMutation, deleteMutation } = useStages(pipelineId)
  const [isRenaming, setIsRenaming] = useState(false)
  const [renameValue, setRenameValue] = useState(stage.name)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const { setNodeRef: setDropRef } = useDroppable({ id: stage.id })

  const cardIds = stage.cards.map((c) => c.id)

  function handleRenameSubmit() {
    const trimmed = renameValue.trim()
    if (trimmed && trimmed !== stage.name) {
      renameMutation.mutate({ id: stage.id, name: trimmed })
    }
    setIsRenaming(false)
  }

  function handleRenameKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") handleRenameSubmit()
    if (e.key === "Escape") {
      setRenameValue(stage.name)
      setIsRenaming(false)
    }
  }

  return (
    <div className="flex flex-col flex-shrink-0 w-[280px]">
      {/* Column header */}
      <div className="flex items-center justify-between mb-2 px-1">
        <div className="flex-1 min-w-0">
          {isRenaming ? (
            <Input
              ref={inputRef}
              value={renameValue}
              onChange={(e) => setRenameValue(e.target.value)}
              onBlur={handleRenameSubmit}
              onKeyDown={handleRenameKeyDown}
              className="h-7 text-sm font-semibold"
              autoFocus
            />
          ) : (
            <div>
              <h3 className="text-sm font-semibold truncate">{stage.name}</h3>
              <p className="text-xs text-muted-foreground">
                {stage.cardCount} card{stage.cardCount !== 1 ? "s" : ""} &middot;{" "}
                {formatCurrency(stage.monetarySum, workspaceCurrency)}
              </p>
            </div>
          )}
        </div>
        {role === "admin" && !isRenaming && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-7 w-7 flex-shrink-0">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Opções do estágio</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => {
                  setRenameValue(stage.name)
                  setIsRenaming(true)
                  setTimeout(() => inputRef.current?.focus(), 0)
                }}
              >
                Renomear
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive focus:text-destructive"
                onClick={() => setShowDeleteDialog(true)}
              >
                Excluir estágio
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>

      {/* Cards lane */}
      <div
        ref={setDropRef}
        className={`bg-muted/50 border border-border rounded-lg p-2 flex flex-col gap-2 overflow-y-auto transition-colors ${
          isOver ? "bg-primary/5 border-primary" : ""
        }`}
        style={{ maxHeight: "calc(100vh - 56px - 56px - 80px)" }}
      >
        <SortableContext items={cardIds} strategy={verticalListSortingStrategy}>
          {stage.cards.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 gap-2">
              <LayoutList className="h-8 w-8 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Sem cards</p>
            </div>
          ) : (
            stage.cards.map((card) => (
              <KanbanCard
                key={card.id}
                card={card}
                stageId={stage.id}
                workspaceCurrency={workspaceCurrency}
              />
            ))
          )}
        </SortableContext>
      </div>

      {/* Delete Stage Dialog */}
      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Excluir estágio?</AlertDialogTitle>
            <AlertDialogDescription>
              Os cards neste estágio serão excluídos. Esta ação não pode ser desfeita.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Manter estágio</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                deleteMutation.mutate(stage.id)
                setShowDeleteDialog(false)
              }}
            >
              Excluir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
