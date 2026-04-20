"use client"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import type { BoardData, Stage, CardSummary } from "@/types/api"

export function useStages(pipelineId: string) {
  const qc = useQueryClient()

  const createMutation = useMutation({
    mutationFn: async (name: string) => {
      const res = await fetch(`/api/pipelines/${pipelineId}/stages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json() as Promise<Stage>
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
  })

  const renameMutation = useMutation({
    mutationFn: async ({ id, name }: { id: string; name: string }) => {
      const res = await fetch(`/api/stages/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onMutate: async ({ id, name }) => {
      await qc.cancelQueries({ queryKey: ["board", pipelineId] })
      const previousBoard = qc.getQueryData<BoardData>(["board", pipelineId])
      qc.setQueryData<BoardData>(["board", pipelineId], (old) => {
        if (!old) return old
        return {
          ...old,
          stages: old.stages.map((s) => (s.id === id ? { ...s, name } : s)),
        }
      })
      return { previousBoard }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previousBoard) qc.setQueryData(["board", pipelineId], ctx.previousBoard)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/stages/${id}`, { method: "DELETE" })
      if (!res.ok) throw new Error(await res.text())
    },
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: ["board", pipelineId] })
      const previousBoard = qc.getQueryData<BoardData>(["board", pipelineId])
      qc.setQueryData<BoardData>(["board", pipelineId], (old) => {
        if (!old) return old
        return { ...old, stages: old.stages.filter((s) => s.id !== id) }
      })
      return { previousBoard }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previousBoard) qc.setQueryData(["board", pipelineId], ctx.previousBoard)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
  })

  const reorderMutation = useMutation({
    mutationFn: async ({
      id,
      prevPosition,
      nextPosition,
    }: {
      id: string
      prevPosition: number | null
      nextPosition: number | null
    }) => {
      const res = await fetch(`/api/stages/${id}/position`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ prevPosition, nextPosition }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onMutate: async ({ id, prevPosition, nextPosition }) => {
      await qc.cancelQueries({ queryKey: ["board", pipelineId] })
      const previousBoard = qc.getQueryData<BoardData>(["board", pipelineId])
      // Optimistic reorder handled by BoardShell local state
      return { previousBoard }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previousBoard) qc.setQueryData(["board", pipelineId], ctx.previousBoard)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
  })

  return { createMutation, renameMutation, deleteMutation, reorderMutation }
}
