"use client"
import { useState } from "react"
import { useParams } from "next/navigation"
import { useMutation } from "@tanstack/react-query"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Loader2, Copy, Check } from "lucide-react"
import type { InviteResponse } from "@/types/api"

export default function InvitePage() {
  const { id } = useParams<{ id: string }>()
  const [email, setEmail] = useState("")
  const [role, setRole] = useState<"admin" | "member">("member")
  const [generated, setGenerated] = useState<InviteResponse | null>(null)
  const [copied, setCopied] = useState(false)

  const mutation = useMutation({
    mutationFn: async () => {
      const res = await fetch(`/api/workspaces/${id}/invites`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, role }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json() as Promise<InviteResponse>
    },
    onSuccess: (data) => { setGenerated(data); setEmail("") },
  })

  const copy = async () => {
    if (!generated) return
    await navigator.clipboard.writeText(generated.inviteUrl)
    setCopied(true); setTimeout(() => setCopied(false), 1500)
  }

  return (
    <Card className="max-w-xl">
      <CardHeader><CardTitle>Convidar membro</CardTitle></CardHeader>
      <CardContent className="space-y-4">
        {mutation.isError && <Alert variant="destructive"><AlertDescription>Erro ao enviar convite. Tente novamente.</AlertDescription></Alert>}
        {generated && (
          <Alert>
            <AlertDescription className="space-y-2">
              <p className="text-sm">Convite enviado. Compartilhe o link abaixo:</p>
              <div className="flex gap-2">
                <Input readOnly value={generated.inviteUrl} className="flex-1 font-mono text-xs" />
                <Button type="button" variant="outline" size="icon" onClick={copy}>{copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}</Button>
              </div>
            </AlertDescription>
          </Alert>
        )}

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="nome@empresa.com" />
        </div>

        <div className="space-y-2">
          <Label htmlFor="role">Papel</Label>
          <Select value={role} onValueChange={(v) => setRole(v as any)}>
            <SelectTrigger id="role"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="member">Membro</SelectItem>
              <SelectItem value="admin">Admin</SelectItem>
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">Admins podem gerenciar membros e pipelines.</p>
        </div>

        <Button onClick={() => mutation.mutate()} disabled={mutation.isPending || !email.includes("@")} className="w-full">
          {mutation.isPending ? (<><Loader2 className="h-4 w-4 animate-spin mr-2" />Enviando...</>) : "Enviar convite"}
        </Button>
      </CardContent>
    </Card>
  )
}
