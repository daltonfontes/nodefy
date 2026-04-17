"use client"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { useMutation } from "@tanstack/react-query"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Loader2 } from "lucide-react"
import { generateSlug } from "@/lib/slug"

export default function NewWorkspacePage() {
  const [name, setName] = useState("")
  const [touched, setTouched] = useState(false)
  const router = useRouter()
  const slug = generateSlug(name)
  const error = touched && name.length < 2 ? (name.length === 0 ? "Nome é obrigatório" : "Nome deve ter pelo menos 2 caracteres") : null

  const mutation = useMutation({
    mutationFn: async (n: string) => {
      const res = await fetch("/api/workspaces/proxy", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: n }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json() as Promise<{ id: string }>
    },
    onSuccess: (ws) => router.push(`/workspace/${ws.id}`),    // D-13: empty board with pipeline CTA
  })

  return (
    <div className="flex min-h-screen items-center justify-center bg-secondary px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Crie seu workspace</CardTitle>
          <p className="text-sm text-muted-foreground">Seu workspace é onde seu time gerencia pipelines e deals.</p>
        </CardHeader>
        <CardContent className="space-y-4">
          {mutation.isError && <Alert variant="destructive"><AlertDescription>Erro ao criar workspace. Tente novamente.</AlertDescription></Alert>}
          <div className="space-y-2">
            <Label htmlFor="name">Nome do workspace</Label>
            <Input id="name" value={name} onChange={(e) => setName(e.target.value)} onBlur={() => setTouched(true)} placeholder="Ex: Vendas — Acme Corp" maxLength={50} />
            {error && <p className="text-xs text-destructive">{error}</p>}
            {!error && slug && <p className="text-xs text-muted-foreground">URL: /{slug}</p>}
            <p className="text-xs text-muted-foreground">Visível para todos os membros do workspace.</p>
          </div>
          <Button className="w-full" disabled={mutation.isPending || name.length < 2} onClick={() => mutation.mutate(name)}>
            {mutation.isPending ? (<><Loader2 className="h-4 w-4 animate-spin mr-2" />Criando...</>) : "Criar workspace"}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
