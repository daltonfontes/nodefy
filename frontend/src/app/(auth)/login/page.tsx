"use client"
import { Suspense, useState } from "react"
import { useSearchParams } from "next/navigation"
import { Card, CardContent } from "@/components/ui/card"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Github, Mail, Building2 } from "lucide-react"
import { Logo } from "@/components/Logo"
import { SsoButton } from "@/components/SsoButton"

function LoginContent() {
  const params = useSearchParams()
  const callbackUrl = params.get("callbackUrl") ?? "/workspace/select"
  const error = params.get("error")
  const [anyLoading, setAnyLoading] = useState(false)

  return (
    <div className="flex min-h-screen items-center justify-center bg-secondary px-4">
      <Card className="w-full max-w-sm">
        <CardContent className="space-y-6 p-8">
          <div className="space-y-2 text-center">
            <Logo className="text-2xl font-semibold" />
            <p className="text-sm text-muted-foreground">CRM para times de vendas</p>
          </div>

          {error && (
            <Alert variant="destructive">
              <AlertDescription>Não foi possível autenticar. Tente novamente.</AlertDescription>
            </Alert>
          )}

          <div className="space-y-3">
            <SsoButton provider="github" label="Continuar com GitHub" icon={<Github className="h-5 w-5" />} callbackUrl={callbackUrl} disabledExternally={anyLoading} onClickStart={() => setAnyLoading(true)} />
            <SsoButton provider="google" label="Continuar com Google" icon={<Mail className="h-5 w-5" />} callbackUrl={callbackUrl} disabledExternally={anyLoading} onClickStart={() => setAnyLoading(true)} />
            <SsoButton provider="microsoft-entra-id" label="Continuar com Microsoft" icon={<Building2 className="h-5 w-5" />} callbackUrl={callbackUrl} disabledExternally={anyLoading} onClickStart={() => setAnyLoading(true)} />
          </div>

          <p className="text-xs text-muted-foreground text-center">
            Ao entrar, você concorda com os Termos de Uso e a Política de Privacidade.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginContent />
    </Suspense>
  )
}
