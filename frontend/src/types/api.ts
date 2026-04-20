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

export interface Pipeline {
  id: string
  name: string
  position: number
  createdAt: string
}

export interface Stage {
  id: string
  pipelineId: string
  name: string
  position: number
  cardCount: number
  monetarySum: number
}

export interface Card {
  id: string
  stageId: string
  pipelineId: string
  title: string
  description: string | null
  monetaryValue: number | null
  assigneeId: string | null
  closeDate: string | null
  position: number
  stageEnteredAt: string
  createdAt: string
  archivedAt: string | null
}

export interface CardSummary {
  id: string
  title: string
  monetaryValue: number | null
  assigneeId: string | null
  stageEnteredAt: string
  position: number
}

export interface BoardData {
  pipeline: Pipeline
  stages: (Stage & { cards: CardSummary[] })[]
}

export interface ActivityLog {
  id: string
  action: "created" | "moved" | "edited" | "archived"
  payload: string
  actorId: string
  createdAt: string
}
