export function getStageAge(stageEnteredAt: string) {
  const diffMs = Date.now() - new Date(stageEnteredAt).getTime()
  const diffDays = Math.floor(diffMs / 86_400_000)
  const diffHours = Math.floor(diffMs / 3_600_000)
  const label = diffDays >= 1 ? `${diffDays}d` : `${diffHours}h`
  const level = diffDays > 30 ? "critical" : diffDays >= 7 ? "warning" : "neutral"
  return { label, level }
}
