export interface Workspace {
  id: string
  name: string
  slug: string
  currency: "BRL" | "USD" | "EUR"
  currencyLocked: boolean
  role: "admin" | "member"
}

export interface Member {
  userId: string
  name: string | null
  email: string
  avatarUrl: string | null
  role: "admin" | "member"
  joinedAt: string
}

export interface InviteResponse {
  inviteUrl: string
  token: string
  expiresAt: string
}

export interface InviteInfo {
  workspaceName: string
  role: "admin" | "member"
}
