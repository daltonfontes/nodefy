"use client"
import { useState } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { MoreHorizontal } from "lucide-react"
import type { Member } from "@/types/api"

export function MemberTable({ workspaceId, initialMembers }: { workspaceId: string; initialMembers: Member[] }) {
  const qc = useQueryClient()
  const { data: members = initialMembers } = useQuery<Member[]>({
    queryKey: ["members", workspaceId],
    queryFn: async () => (await fetch(`/api/workspaces/${workspaceId}/members`)).json(),
    initialData: initialMembers,
  })
  const [confirmRemove, setConfirmRemove] = useState<Member | null>(null)

  const roleMutation = useMutation({
    mutationFn: async ({ userId, role }: { userId: string; role: "admin" | "member" }) => {
      const res = await fetch(`/api/workspaces/${workspaceId}/members/${userId}`, {
        method: "PATCH", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ role }),
      })
      if (!res.ok) throw new Error(await res.text())
      return res.json()
    },
    onMutate: async ({ userId, role }) => {
      await qc.cancelQueries({ queryKey: ["members", workspaceId] })
      const previous = qc.getQueryData<Member[]>(["members", workspaceId])
      qc.setQueryData<Member[]>(["members", workspaceId], (old) => old?.map((m) => m.userId === userId ? { ...m, role } : m) ?? [])
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(["members", workspaceId], ctx.previous)
      alert("Não foi possível alterar o papel. Tente novamente.")
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["members", workspaceId] }),
  })

  const removeMutation = useMutation({
    mutationFn: async (userId: string) => {
      const res = await fetch(`/api/workspaces/${workspaceId}/members/${userId}`, { method: "DELETE" })
      if (!res.ok) throw new Error(await res.text())
    },
    onMutate: async (userId) => {
      await qc.cancelQueries({ queryKey: ["members", workspaceId] })
      const previous = qc.getQueryData<Member[]>(["members", workspaceId])
      qc.setQueryData<Member[]>(["members", workspaceId], (old) => old?.filter((m) => m.userId !== userId) ?? [])
      return { previous }
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.previous) qc.setQueryData(["members", workspaceId], ctx.previous)
      alert("Não foi possível remover o membro.")
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["members", workspaceId] }),
  })

  return (
    <>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Membro</TableHead>
            <TableHead>Papel</TableHead>
            <TableHead className="hidden sm:table-cell">Entrou em</TableHead>
            <TableHead className="w-[60px]"></TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {members.map((m) => (
            <TableRow key={m.userId}>
              <TableCell>
                <div className="flex items-center gap-3">
                  <Avatar className="h-8 w-8">
                    {m.avatarUrl && <AvatarImage src={m.avatarUrl} />}
                    <AvatarFallback>{(m.name ?? m.email)[0]?.toUpperCase()}</AvatarFallback>
                  </Avatar>
                  <div>
                    <div className="text-sm font-medium">{m.name ?? "—"}</div>
                    <div className="text-xs text-muted-foreground">{m.email}</div>
                  </div>
                </div>
              </TableCell>
              <TableCell>
                <Badge variant={m.role === "admin" ? "default" : "secondary"}>
                  {m.role === "admin" ? "Admin" : "Membro"}
                </Badge>
              </TableCell>
              <TableCell className="hidden sm:table-cell text-sm text-muted-foreground">
                {new Date(m.joinedAt).toLocaleDateString("pt-BR")}
              </TableCell>
              <TableCell>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-8 w-8"><MoreHorizontal className="h-4 w-4" /></Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {m.role === "member" ? (
                      <DropdownMenuItem onClick={() => roleMutation.mutate({ userId: m.userId, role: "admin" })}>Tornar admin</DropdownMenuItem>
                    ) : (
                      <DropdownMenuItem onClick={() => roleMutation.mutate({ userId: m.userId, role: "member" })}>Tornar membro</DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => setConfirmRemove(m)}>
                      Remover do workspace
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={!!confirmRemove} onOpenChange={(o) => !o && setConfirmRemove(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Remover membro</DialogTitle>
            <DialogDescription>
              Tem certeza que deseja remover {confirmRemove?.name ?? confirmRemove?.email} do workspace? Eles perderão acesso imediatamente.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmRemove(null)}>Cancelar</Button>
            <Button variant="destructive" onClick={() => { if (confirmRemove) { removeMutation.mutate(confirmRemove.userId); setConfirmRemove(null) } }}>Remover</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
