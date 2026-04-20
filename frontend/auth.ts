import NextAuth from "next-auth"
import { authConfig } from "./auth.config"

export const { handlers, auth, signIn, signOut } = NextAuth({
  ...authConfig,
  callbacks: {
    ...authConfig.callbacks,
    async signIn({ user, account }) {
      // After successful OAuth, sync user with backend so we have a stable users.id
      if (!user.email) {
        // GitHub fallback failed — refuse login (D-12 surfaces inline error)
        return false
      }
      try {
        const apiUrl =
          process.env.INTERNAL_API_URL ??
          process.env.NEXT_PUBLIC_API_URL ??
          "http://localhost:5000"
        // We need a JWT to call /sso/sync — mint a short-lived token signed with AUTH_SECRET
        const { mintApiToken } = await import("@/lib/api-token")
        const token = await mintApiToken({ sub: user.id ?? account?.providerAccountId ?? "", email: user.email })
        const res = await fetch(`${apiUrl}/sso/sync`, {
          method: "POST",
          headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
          body: JSON.stringify({
            provider: account?.provider ?? "unknown",
            providerAccountId: account?.providerAccountId ?? "",
            email: user.email,
            name: user.name,
            avatarUrl: user.image,
          }),
        })
        if (!res.ok) return false
        const dto = (await res.json()) as { id: string }
        // Replace the user.id with the canonical backend users.id so subsequent JWTs use it
        user.id = dto.id
        return true
      } catch {
        return false
      }
    },
    async jwt({ token, account, user }) {
      if (account) {
        token.provider = account.provider
        token.providerAccountId = account.providerAccountId
      }
      if (user?.id) token.sub = user.id    // canonical backend id
      return token
    },
  },
})
