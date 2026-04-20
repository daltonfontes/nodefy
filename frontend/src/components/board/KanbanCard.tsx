"use client"
import { useSortable } from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { useRouter } from "next/navigation"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { getStageAge } from "@/lib/stage-age"
import type { CardSummary } from "@/types/api"

interface KanbanCardProps {
  card: CardSummary
  stageId: string
  workspaceCurrency: "BRL" | "USD" | "EUR"
}

function formatCurrency(value: number, currency: "BRL" | "USD" | "EUR") {
  const localeMap = { BRL: "pt-BR", USD: "en-US", EUR: "de-DE" }
  return new Intl.NumberFormat(localeMap[currency], {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(value)
}

export function KanbanCard({ card, stageId, workspaceCurrency }: KanbanCardProps) {
  const router = useRouter()
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { card, stageId },
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.4 : 1,
  }

  const { label: ageLabel, level: ageLevel } = getStageAge(card.stageEnteredAt)

  const ageBadgeClass =
    ageLevel === "critical"
      ? "text-xs font-semibold px-1.5 py-0.5 rounded-full bg-red-100 text-red-600"
      : ageLevel === "warning"
        ? "text-xs font-semibold px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700"
        : "text-xs font-semibold px-1.5 py-0.5 rounded-full bg-slate-100 text-slate-500"

  function handleClick() {
    if (isDragging) return
    router.push(`?card=${card.id}`)
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      onClick={handleClick}
      className="bg-card border border-border rounded-lg p-3 shadow-sm cursor-grab hover:shadow-md active:cursor-grabbing"
    >
      <p className="text-sm text-foreground line-clamp-2 mb-2">{card.title}</p>
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          {card.assigneeId && (
            <Avatar className="h-6 w-6">
              <AvatarFallback className="text-[10px]">
                {card.assigneeId.slice(0, 2).toUpperCase()}
              </AvatarFallback>
            </Avatar>
          )}
          {card.monetaryValue != null && (
            <span className="text-sm text-muted-foreground">
              {formatCurrency(card.monetaryValue, workspaceCurrency)}
            </span>
          )}
        </div>
        <span className={ageBadgeClass}>{ageLabel}</span>
      </div>
    </div>
  )
}
