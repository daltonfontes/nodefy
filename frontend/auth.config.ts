import type { NextAuthConfig } from "next-auth"
import GitHub from "next-auth/providers/github"
import Google from "next-auth/providers/google"
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id"

export const authConfig = {
  trustHost: true,
  providers: [
    GitHub({
      authorization: { params: { scope: "read:user user:email" } },
      // CRITICAL: Pitfall 1 — GitHub primary email can be null.
      // Override profile() to fetch verified email from /user/emails when null.
      async profile(profile, tokens) {
        let email: string | null = profile.email ?? null
        if (!email) {
          const res = await fetch("https://api.github.com/user/emails", {
            headers: { Authorization: `token ${tokens.access_token}` },
          })
          if (res.ok) {
            const emails = (await res.json()) as Array<{ email: string; primary: boolean; verified: boolean }>
            const verifiedPrimary = emails.find((e) => e.primary && e.verified) ?? emails.find((e) => e.verified)
            email = verifiedPrimary?.email ?? null
          }
        }
        return {
          id: String(profile.id),
          name: profile.name ?? profile.login,
          email,
          image: profile.avatar_url,
        }
      },
    }),
    Google,
    MicrosoftEntraID,
  ],
  pages: { signIn: "/login", error: "/login" },
  session: { strategy: "jwt" },
  callbacks: {
    async jwt({ token, account, profile }) {
      if (account) {
        token.provider = account.provider
        token.providerAccountId = account.providerAccountId
        token.providerUserId = (profile as any)?.id ?? account.providerAccountId
      }
      return token
    },
    async session({ session, token }) {
      if (token.sub) session.user.id = token.sub
      ;(session as any).provider = token.provider
      ;(session as any).providerAccountId = token.providerAccountId
      return session
    },
    async authorized({ auth, request }) {
      // Used by proxy.ts middleware
      const isLoggedIn = !!auth?.user
      const path = request.nextUrl.pathname
      if (path === "/login" || path.startsWith("/invite/") || path.startsWith("/api/auth/")) return true
      return isLoggedIn
    },
  },
} satisfies NextAuthConfig
