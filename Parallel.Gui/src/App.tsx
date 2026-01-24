import { Footer } from "./components/footer.tsx";
import {Header} from "./components/header.tsx";
import {useEffect, useState} from "react";
import {API_URL} from "@/lib/config.ts";
import type {VaultConfig} from "@/lib/types.ts";
import {RefreshCw} from "lucide-react";

// Main App component - this is the root of our application
export default function App(){
    const [activeVaultId, setActiveVaultId] = useState<string | null>(null)
    const [editingVault, setEditingVault] = useState<VaultConfig | null>(null)
    const [vaultDialogOpen, setVaultDialogOpen] = useState(false)
    const [vaults, setVaults] = useState<VaultConfig[]>([])
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        const loadVaults = async () => {
            try {
                const res = await fetch(`${API_URL}/vaults`, { credentials: "include" })
                if (!res.ok) console.error("Failed to fetch vaults")

                const data: VaultConfig[] = await res.json()
                setVaults(data)

                // Select first vault by default
                if (data.length > 0) {
                    setActiveVaultId(data[0].id)
                }
            } catch (err) {
                console.error(err)
            } finally {
                setLoading(false)
            }
        }

        loadVaults()
    }, [])

    const handleSetVault = (id: string) => {
        setActiveVaultId(id)
    }

    const handleAddVault = () => {
        setEditingVault(null)
        setVaultDialogOpen(true)
    }

    const handleEditVault = (vault: VaultConfig) => {
        setEditingVault(vault)
        setVaultDialogOpen(true)
    }

    return (
        <div className="h-screen flex flex-col bg-background">
            <Header
                activeVaultId={activeVaultId}
                vaults={vaults}
                onSetVault={handleSetVault}
                onAddVault={handleAddVault}
                onEditVault={handleEditVault}
                onMinimize={() => window.electronAPI.minimize()}
                onMaximize={() => window.electronAPI.maximize()}
                onClose={() => window.electronAPI.close()}
            />

            {loading ? (
                <div className="flex-1 flex flex-col items-center justify-center gap-4 bg-background">
                    <RefreshCw className="w-10 h-10 text-primary animate-spin" />
                    <span className="text-2xl font-semibold text-primary animate-pulse">Loading...</span>
                </div>
            ) : (
                <div className="flex-1 overflow-hidden">
                </div>
            )}

            <div className="flex-1 overflow-hidden">

            </div>

            <Footer
                activeVaultId={activeVaultId}
            />
        </div>
    );
}
