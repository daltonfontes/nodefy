"use client"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import type { Pipeline } from "@/types/api"

export function usePipelines(workspaceId: string) {
  const qc = useQueryClient()

  const { data: pipelines = [] } = useQuery<Pipeline[]>({
    queryKey: ["pipelines", workspaceId],
    queryFn: async () => {
      const res = await fetch(`/api/workspaces/${workspaceId}/pipelines`)
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    staleTime: 60_000,
  })

  const createMutation = useMutation({
    mutationFn: async (name: string) => {
      const res = await fetch(`/api/workspaces/${workspaceId}/pipelines`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json() as Promise<Pipeline>
    },
    onMutate: async (name) => {
      await qc.cancelQueries({ queryKey: ["pipelines", workspaceId] })
      const previous = qc.getQueryData<Pipeline[]>(["pipelines", workspaceId])
      const optimistic: Pipeline = {
        id: `temp-${Date.now()}`,
        name,
        position: (previous?.length ?? 0) + 1,
        createdAt: new Date().toISOString(),
      }
      qc.setQueryData<Pipeline[]>(["pipelines", workspaceId], (old) => [...(old ?? []), optimistic])
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(["pipelines", workspaceId], ctx.previous)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["pipelines", workspaceId] }),
  })

  const renameMutation = useMutation({
    mutationFn: async ({ id, name }: { id: string; name: string }) => {
      const res = await fetch(`/api/pipelines/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onMutate: async ({ id, name }) => {
      await qc.cancelQueries({ queryKey: ["pipelines", workspaceId] })
      const previous = qc.getQueryData<Pipeline[]>(["pipelines", workspaceId])
      qc.setQueryData<Pipeline[]>(["pipelines", workspaceId], (old) =>
        old?.map((p) => (p.id === id ? { ...p, name } : p)) ?? []
      )
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(["pipelines", workspaceId], ctx.previous)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["pipelines", workspaceId] }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/pipelines/${id}`, { method: "DELETE" })
      if (!res.ok) throw new Error(await res.text())
    },
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: ["pipelines", workspaceId] })
      const previous = qc.getQueryData<Pipeline[]>(["pipelines", workspaceId])
      qc.setQueryData<Pipeline[]>(["pipelines", workspaceId], (old) =>
        old?.filter((p) => p.id !== id) ?? []
      )
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(["pipelines", workspaceId], ctx.previous)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["pipelines", workspaceId] }),
  })

  return { pipelines, createMutation, renameMutation, deleteMutation }
}
