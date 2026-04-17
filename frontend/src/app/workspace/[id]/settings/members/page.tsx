import Link from "next/link"
import { apiFetch } from "@/lib/api"
import type { Member } from "@/types/api"
import { Button } from "@/components/ui/button"
import { MemberTable } from "./MemberTable"

export default async function MembersPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  const members = await apiFetch<Member[]>(`/workspaces/${id}/members`, { tenantId: id })
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Membros</h1>
          <p className="text-sm text-muted-foreground">{members.length} {members.length === 1 ? "membro" : "membros"}</p>
        </div>
        <Link href={`/workspace/${id}/settings/members/invite`}>
          <Button size="sm">Convidar membro</Button>
        </Link>
      </div>
      <MemberTable workspaceId={id} initialMembers={members} />
    </div>
  )
}
