"use client"
import { useState, useRef } from "react"
import { useSearchParams, useRouter } from "next/navigation"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
import { getStageAge } from "@/lib/stage-age"
import type { Card, ActivityLog } from "@/types/api"

interface CardDetailPanelProps {
  workspaceId: string
  pipelineId: string
  workspaceCurrency: "BRL" | "USD" | "EUR"
}

function formatRelativeTime(dateStr: string) {
  const diffMs = Date.now() - new Date(dateStr).getTime()
  const diffSeconds = Math.floor(diffMs / 1000)
  const diffMinutes = Math.floor(diffSeconds / 60)
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)

  try {
    const rtf = new Intl.RelativeTimeFormat("pt-BR", { numeric: "auto" })
    if (diffDays > 0) return rtf.format(-diffDays, "day")
    if (diffHours > 0) return rtf.format(-diffHours, "hour")
    if (diffMinutes > 0) return rtf.format(-diffMinutes, "minute")
    return rtf.format(-diffSeconds, "second")
  } catch {
    return new Date(dateStr).toLocaleDateString("pt-BR")
  }
}

function activityLabel(log: ActivityLog) {
  let parsed: Record<string, unknown> = {}
  try { parsed = JSON.parse(log.payload) } catch { /* ignore */ }

  switch (log.action) {
    case "created": return "Card criado"
    case "moved":
      return `Card movido: ${parsed.from_stage ?? "?"} → ${parsed.to_stage ?? "?"}`
    case "edited":
      return `Campo editado: ${parsed.field ?? "?"}`
    case "archived": return "Card arquivado"
    default: return log.action
  }
}

export function CardDetailPanel({ workspaceId, pipelineId, workspaceCurrency }: CardDetailPanelProps) {
  const searchParams = useSearchParams()
  const router = useRouter()
  const qc = useQueryClient()
  const openCardId = searchParams.get("card")

  const [showArchiveDialog, setShowArchiveDialog] = useState(false)
  const [isEditingTitle, setIsEditingTitle] = useState(false)
  const [titleValue, setTitleValue] = useState("")

  const { data: card } = useQuery<Card>({
    queryKey: ["card", openCardId],
    queryFn: async () => {
      const res = await fetch(`/api/cards/${openCardId}?workspaceId=${workspaceId}`)
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    enabled: !!openCardId,
  })

  const { data: activity = [] } = useQuery<ActivityLog[]>({
    queryKey: ["activity", openCardId],
    queryFn: async () => {
      const res = await fetch(`/api/cards/${openCardId}/activity`)
      if (!res.ok) return []
      return res.json()
    },
    enabled: !!openCardId,
  })

  const patchMutation = useMutation({
    mutationFn: async (patch: Partial<Pick<Card, "title" | "description" | "monetaryValue" | "assigneeId" | "closeDate">>) => {
      const res = await fetch(`/api/cards/${openCardId}?workspaceId=${workspaceId}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(patch),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: ["card", openCardId] })
      qc.invalidateQueries({ queryKey: ["board", pipelineId] })
    },
  })

  const archiveMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch(`/api/cards/${openCardId}/archive?workspaceId=${workspaceId}`, { method: "PATCH" })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onSuccess: () => {
      closePanel()
      qc.invalidateQueries({ queryKey: ["board", pipelineId] })
    },
  })

  function closePanel() {
    const p = new URLSearchParams(searchParams.toString())
    p.delete("card")
    const qs = p.toString()
    router.replace(qs ? "?" + qs : window.location.pathname)
  }

  function handleTitleBlur() {
    const trimmed = titleValue.trim()
    if (trimmed && card && trimmed !== card.title) {
      patchMutation.mutate({ title: trimmed })
    }
    setIsEditingTitle(false)
  }

  function handleTitleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") handleTitleBlur()
    if (e.key === "Escape") setIsEditingTitle(false)
  }

  const stageAge = card ? getStageAge(card.stageEnteredAt) : null

  return (
    <>
      <Sheet open={!!openCardId} onOpenChange={(open) => { if (!open) closePanel() }}>
        <SheetContent side="right" className="w-[480px] sm:w-[480px] overflow-y-auto">
          <SheetHeader className="mb-4">
            {card ? (
              isEditingTitle ? (
                <Input
                  value={titleValue}
                  onChange={(e) => setTitleValue(e.target.value)}
                  onBlur={handleTitleBlur}
                  onKeyDown={handleTitleKeyDown}
                  className="text-lg font-semibold"
                  autoFocus
                />
              ) : (
                <SheetTitle
                  className="text-lg font-semibold cursor-pointer hover:underline"
                  onClick={() => {
                    setTitleValue(card.title)
                    setIsEditingTitle(true)
                  }}
                >
                  {card.title}
                </SheetTitle>
              )
            ) : (
              <SheetTitle className="text-muted-foreground">Carregando...</SheetTitle>
            )}
          </SheetHeader>

          {card && (
            <div className="flex flex-col gap-4">
              {/* Stage age */}
              {stageAge && (
                <div className="flex items-center gap-2 text-sm">
                  <span className="text-muted-foreground">No estágio há</span>
                  <span className="font-medium">{stageAge.label}</span>
                </div>
              )}

              {/* Responsável */}
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Responsável
                </label>
                <p className="text-sm">{card.assigneeId ?? "—"}</p>
              </div>

              {/* Data de fechamento */}
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Data de fechamento
                </label>
                <p className="text-sm">
                  {card.closeDate
                    ? new Date(card.closeDate).toLocaleDateString("pt-BR")
                    : "—"}
                </p>
              </div>

              {/* Valor */}
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Valor
                </label>
                <p className="text-sm">
                  {card.monetaryValue != null
                    ? new Intl.NumberFormat("pt-BR", { style: "currency", currency: workspaceCurrency }).format(card.monetaryValue)
                    : "—"}
                </p>
              </div>

              {/* Descrição */}
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  Descrição
                </label>
                <textarea
                  defaultValue={card.description ?? ""}
                  placeholder="Adicionar descrição..."
                  className="text-sm bg-transparent border border-border rounded p-2 resize-none min-h-[80px] focus:outline-none focus:ring-2 focus:ring-ring"
                  onBlur={(e) => {
                    const val = e.target.value.trim()
                    if (val !== (card.description ?? "")) {
                      patchMutation.mutate({ description: val || null })
                    }
                  }}
                />
              </div>

              {/* Atividade */}
              {activity.length > 0 && (
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Atividade
                  </label>
                  <ol className="flex flex-col gap-2">
                    {activity.map((log) => (
                      <li key={log.id} className="flex items-start gap-2 text-sm">
                        <span className="text-muted-foreground mt-0.5">●</span>
                        <div>
                          <span>{activityLabel(log)}</span>
                          <span className="text-xs text-muted-foreground ml-2">
                            {formatRelativeTime(log.createdAt)}
                          </span>
                        </div>
                      </li>
                    ))}
                  </ol>
                </div>
              )}

              {/* Archive */}
              <div className="pt-4 border-t border-border">
                <Button
                  variant="ghost"
                  className="text-destructive hover:text-destructive hover:bg-destructive/10 w-full"
                  onClick={() => setShowArchiveDialog(true)}
                >
                  Arquivar card
                </Button>
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>

      <AlertDialog open={showArchiveDialog} onOpenChange={setShowArchiveDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Arquivar card?</AlertDialogTitle>
            <AlertDialogDescription>
              Card arquivado? Ele não aparecerá mais no board.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Manter card</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                archiveMutation.mutate()
                setShowArchiveDialog(false)
              }}
            >
              Arquivar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
