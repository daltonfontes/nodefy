import { auth } from "@/auth"
import { redirect } from "next/navigation"
import Link from "next/link"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription } from "@/components/ui/alert"

const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000"

async function getInviteInfo(token: string): Promise<{ workspaceName: string; role: string } | { error: number }> {
  const res = await fetch(`${baseUrl}/invites/${token}`, { cache: "no-store" })
  if (!res.ok) return { error: res.status }
  return await res.json()
}

export default async function InviteAcceptPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params
  const session = await auth()
  if (!session) redirect(`/login?callbackUrl=/invite/${token}`)

  const info = await getInviteInfo(token)
  if ("error" in info) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-secondary px-4">
        <Card className="w-full max-w-sm">
          <CardHeader><CardTitle>Link inválido ou expirado</CardTitle></CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">Este convite não é mais válido. Peça ao admin do workspace um novo convite.</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-secondary px-4">
      <Card className="w-full max-w-sm">
        <CardHeader><CardTitle>Aceitar convite para {info.workspaceName}</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm">Você será adicionado como <strong>{info.role === "admin" ? "Admin" : "Membro"}</strong> neste workspace.</p>
          <form action={`/api/invites/${token}/accept`} method="post">
            <Button type="submit" className="w-full">Aceitar convite</Button>
          </form>
          <Link href="/workspace/select" className="text-xs text-muted-foreground underline block text-center">Recusar</Link>
        </CardContent>
      </Card>
    </div>
  )
}
