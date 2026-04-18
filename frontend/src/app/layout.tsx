import type { Metadata } from "next"
import { Inter } from "next/font/google"
import { Providers } from "@/components/Providers"
import "./globals.css"

const inter = Inter({ subsets: ["latin"], variable: "--font-inter" })

export const metadata: Metadata = {
  title: "Nodefy — CRM para times de vendas",
  description: "Pipelines visuais e gestão de deals em tempo real.",
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR" className={inter.variable}>
      <body className="font-sans antialiased bg-background text-foreground">
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}
