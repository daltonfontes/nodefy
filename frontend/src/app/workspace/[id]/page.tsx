import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"

export default function WorkspaceHomePage() {
  return (
    <Card>
      <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
        <h2 className="text-xl font-semibold">Comece criando seu primeiro pipeline</h2>
        <p className="text-sm text-muted-foreground">Pipelines organizam seus deals em estágios. (Disponível na próxima fase.)</p>
        <Button disabled>Crie seu primeiro pipeline</Button>
      </CardContent>
    </Card>
  )
}
