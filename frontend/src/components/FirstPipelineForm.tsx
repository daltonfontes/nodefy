"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { Pipeline } from "@/types/api"

export function FirstPipelineForm({ workspaceId }: { workspaceId: string }) {
  const router = useRouter()
  const [name, setName] = useState("")
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit() {
    const trimmed = name.trim()
    if (!trimmed) {
      toast.error("Informe um nome para o pipeline")
      return
    }
    setSubmitting(true)
    try {
      const res = await fetch(`/api/workspaces/${workspaceId}/pipelines`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: trimmed }),
      })
      if (!res.ok) {
        const text = await res.text().catch(() => "")
        toast.error(`Falha ao criar pipeline: ${text || res.status}`)
        setSubmitting(false)
        return
      }
      const pipeline = (await res.json()) as Pipeline
      router.push(`/workspace/${workspaceId}/pipeline/${pipeline.id}`)
    } catch (err) {
      toast.error(`Falha ao criar pipeline: ${(err as Error).message}`)
      setSubmitting(false)
    }
  }

  return (
    <form
      className="flex flex-col gap-3"
      onSubmit={(e) => {
        e.preventDefault()
        handleSubmit()
      }}
    >
      <Input
        name="name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Ex: Vendas, Onboarding..."
        maxLength={80}
        autoFocus
        disabled={submitting}
        required
      />
      <Button type="submit" disabled={submitting}>
        {submitting ? "Criando..." : "Criar pipeline"}
      </Button>
    </form>
  )
}
