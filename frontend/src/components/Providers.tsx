"use client"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { SessionProvider } from "next-auth/react"
import { useState } from "react"
import { Toaster } from "sonner"

export function Providers({ children }: { children: React.ReactNode }) {
  // useState(() => new QueryClient()) ensures one client per request — NOT shared across users in SSR
  const [queryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: 60_000 } } })
  )
  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        {children}
        <Toaster richColors position="bottom-right" />
      </QueryClientProvider>
    </SessionProvider>
  )
}
