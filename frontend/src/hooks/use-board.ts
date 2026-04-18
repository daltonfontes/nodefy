"use client"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import type { BoardData, CardSummary } from "@/types/api"

export function useBoard(pipelineId: string, workspaceId: string, initialData: BoardData) {
  const qc = useQueryClient()

  const { data: board = initialData } = useQuery<BoardData>({
    queryKey: ["board", pipelineId],
    queryFn: async () => {
      const res = await fetch(`/api/pipelines/${pipelineId}/board?workspaceId=${workspaceId}`)
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    initialData,
    staleTime: 30_000,
  })

  const moveMutation = useMutation({
    mutationFn: async ({
      cardId,
      targetStageId,
      prevPosition,
      nextPosition,
    }: {
      cardId: string
      targetStageId: string
      prevPosition: number | null
      nextPosition: number | null
    }) => {
      const res = await fetch(`/api/cards/${cardId}/move`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ targetStageId, prevPosition, nextPosition }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onMutate: async ({ cardId, targetStageId }) => {
      await qc.cancelQueries({ queryKey: ["board", pipelineId] })
      const previousBoard = qc.getQueryData<BoardData>(["board", pipelineId])
      qc.setQueryData<BoardData>(["board", pipelineId], (old) => {
        if (!old) return old
        const card = old.stages.flatMap((s) => s.cards).find((c) => c.id === cardId)
        if (!card) return old
        const sourceStageId = old.stages.find((s) => s.cards.some((c) => c.id === cardId))?.id
        return {
          ...old,
          stages: old.stages.map((s) => ({
            ...s,
            cards:
              s.id === sourceStageId
                ? s.cards.filter((c) => c.id !== cardId)
                : s.id === targetStageId
                  ? [...s.cards, { ...card }]
                  : s.cards,
          })),
        }
      })
      return { previousBoard }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previousBoard) qc.setQueryData(["board", pipelineId], ctx.previousBoard)
      // Toast shown by BoardShell via moveMutation.isError
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["board", pipelineId] }),
  })

  return { board, moveMutation }
}
