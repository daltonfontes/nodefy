import NextAuth from "next-auth"
import { authConfig } from "./auth.config"

export const { auth: proxy } = NextAuth(authConfig)

export const config = {
  // Allow login, invite landing, auth APIs, static assets
  matcher: ["/((?!api/auth|_next/static|_next/image|favicon.ico|login|invite).*)"],
}
