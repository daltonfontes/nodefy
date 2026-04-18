import { SignJWT } from "jose"

export async function mintApiToken(claims: { sub: string; email: string; tenant_id?: string }) {
  const secret = process.env.AUTH_SECRET
  if (!secret) throw new Error("AUTH_SECRET is required")
  const key = new TextEncoder().encode(secret)
  return await new SignJWT(claims)
    .setProtectedHeader({ alg: "HS256" })
    .setIssuer("nodefy-frontend")
    .setAudience("nodefy-api")
    .setIssuedAt()
    .setExpirationTime("1h")
    .setSubject(claims.sub)
    .sign(key)
}
