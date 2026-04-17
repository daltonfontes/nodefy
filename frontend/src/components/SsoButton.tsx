"use client"
import { signIn } from "next-auth/react"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Loader2 } from "lucide-react"

export function SsoButton({
  provider,
  label,
  icon,
  callbackUrl,
  disabledExternally,
  onClickStart,
}: {
  provider: "github" | "google" | "microsoft-entra-id"
  label: string
  icon: React.ReactNode
  callbackUrl: string
  disabledExternally: boolean
  onClickStart: () => void
}) {
  const [loading, setLoading] = useState(false)
  return (
    <Button
      type="button"
      variant="outline"
      className="w-full justify-start gap-3 h-11"
      disabled={loading || disabledExternally}
      onClick={() => { setLoading(true); onClickStart(); signIn(provider, { callbackUrl }) }}
    >
      {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : icon}
      <span>{loading ? "Conectando..." : label}</span>
    </Button>
  )
}
