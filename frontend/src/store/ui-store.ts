import { create } from "zustand"
import { persist } from "zustand/middleware"

interface UIState {
  // Existing (from Phase 1)
  activeWorkspaceId: string | null
  setActiveWorkspace: (id: string | null) => void

  // Phase 2 additions
  activePipelineId: string | null
  setActivePipelineId: (id: string | null) => void

  sidebarCollapsed: boolean
  setSidebarCollapsed: (collapsed: boolean) => void

  draggingCardId: string | null
  setDraggingCardId: (id: string | null) => void
}

export const useUIStore = create<UIState>()(
  persist(
    (set) => ({
      activeWorkspaceId: null,
      setActiveWorkspace: (id) => set({ activeWorkspaceId: id }),

      activePipelineId: null,
      setActivePipelineId: (id) => set({ activePipelineId: id }),

      sidebarCollapsed: false,
      setSidebarCollapsed: (collapsed) => set({ sidebarCollapsed: collapsed }),

      draggingCardId: null,
      setDraggingCardId: (id) => set({ draggingCardId: id }),
    }),
    {
      name: "nodefy_sidebar_collapsed",
      partialize: (s) => ({ sidebarCollapsed: s.sidebarCollapsed }),
    }
  )
)
