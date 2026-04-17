import Link from "next/link"
import { LogoutButton } from "./LogoutButton"
import { Logo } from "./Logo"

export function WorkspaceTopNav({ workspaceId, workspaceName }: { workspaceId: string; workspaceName: string }) {
  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex max-w-6xl items-center justify-between p-4">
        <div className="flex items-center gap-4">
          <Logo className="font-semibold" />
          <span className="text-sm text-muted-foreground">/ {workspaceName}</span>
        </div>
        <nav className="flex items-center gap-4 text-sm">
          <Link href={`/workspace/${workspaceId}/settings/members`} className="text-muted-foreground hover:text-foreground">Membros</Link>
          <LogoutButton />
        </nav>
      </div>
    </header>
  )
}
