"use client"
import { useState, useRef } from "react"
import { useRouter } from "next/navigation"
import { MoreHorizontal } from "lucide-react"
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
import { usePipelines } from "@/hooks/use-pipelines"
import type { Pipeline } from "@/types/api"

interface PipelineListItemProps {
  pipeline: Pipeline
  isActive: boolean
  workspaceId: string
  role: "admin" | "member"
  collapsed: boolean
}

export function PipelineListItem({
  pipeline,
  isActive,
  workspaceId,
  role,
  collapsed,
}: PipelineListItemProps) {
  const router = useRouter()
  const { renameMutation, deleteMutation } = usePipelines(workspaceId)
  const [isRenaming, setIsRenaming] = useState(false)
  const [renameValue, setRenameValue] = useState(pipeline.name)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  function handleRenameSubmit() {
    const trimmed = renameValue.trim()
    if (trimmed && trimmed !== pipeline.name) {
      renameMutation.mutate({ id: pipeline.id, name: trimmed })
    }
    setIsRenaming(false)
  }

  function handleRenameKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") handleRenameSubmit()
    if (e.key === "Escape") {
      setRenameValue(pipeline.name)
      setIsRenaming(false)
    }
  }

  const activeClass = isActive
    ? "border-l-2 border-primary bg-accent text-accent-foreground font-semibold"
    : "text-foreground hover:bg-accent/50"

  return (
    <>
      <div
        className={`group flex items-center justify-between px-3 py-2 rounded-md cursor-pointer transition-colors ${activeClass}`}
        onClick={() => {
          if (!isRenaming) {
            router.push(`/workspace/${workspaceId}/pipeline/${pipeline.id}`)
          }
        }}
      >
        {isRenaming ? (
          <Input
            ref={inputRef}
            value={renameValue}
            onChange={(e) => setRenameValue(e.target.value)}
            onBlur={handleRenameSubmit}
            onKeyDown={handleRenameKeyDown}
            className="h-6 text-sm py-0 px-1"
            autoFocus
            onClick={(e) => e.stopPropagation()}
          />
        ) : (
          <span className={`text-sm truncate flex-1 ${collapsed ? "hidden" : ""}`}>
            {pipeline.name}
          </span>
        )}

        {role === "admin" && !collapsed && !isRenaming && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                className="opacity-0 group-hover:opacity-100 transition-opacity h-6 w-6 flex items-center justify-center rounded hover:bg-accent ml-1"
                onClick={(e) => e.stopPropagation()}
                aria-label="Opções do pipeline"
              >
                <MoreHorizontal className="h-3.5 w-3.5" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-44">
              <DropdownMenuItem
                onClick={(e) => {
                  e.stopPropagation()
                  setRenameValue(pipeline.name)
                  setIsRenaming(true)
                  setTimeout(() => inputRef.current?.focus(), 0)
                }}
              >
                Renomear
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive focus:text-destructive"
                onClick={(e) => {
                  e.stopPropagation()
                  setShowDeleteDialog(true)
                }}
              >
                Excluir pipeline
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Excluir pipeline?</AlertDialogTitle>
            <AlertDialogDescription>
              Todos os estágios e cards deste pipeline serão excluídos permanentemente. Esta ação
              não pode ser desfeita.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Manter pipeline</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                deleteMutation.mutate(pipeline.id)
                setShowDeleteDialog(false)
                if (isActive) {
                  router.push(`/workspace/${workspaceId}`)
                }
              }}
            >
              Excluir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
