import { Footer } from "./components/footer.tsx";
import {Header} from "./components/header.tsx";
import {useEffect, useState} from "react";
import {API_URL} from "@/lib/config.ts";
import type {VaultConfig} from "@/lib/types.ts";
import {RefreshCw} from "lucide-react";
import {VaultDialog} from "@/components/vault-dialog.tsx";
import {client} from "@/lib/messageClient.ts";



// Main App component - this is the root of our application
export default function App(){
    const [loading, setLoading] = useState(true)
    const [isConnected, setIsConnected] = useState(false);
    const [vaults, setVaults] = useState<VaultConfig[]>([])
    const [activeVaultId, setActiveVaultId] = useState<string | null>(null)
    const [vaultDialogOpen, setVaultDialogOpen] = useState(false)
    const [editingVault, setEditingVault] = useState<VaultConfig | null>(null)
    const loadVaults = async () => {
        try {
            console.log("Loading Vaults...");

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

    useEffect(() => {
        (async () => {
            try {
                await client.connect()
                setIsConnected(client.isConnected())
            } catch (err) {
                console.error("SignalR connection failed:", err)
                setIsConnected(false)
            }
        })()

        client.connectionChanged((connected) => {
            if(connected) loadVaults()
            setIsConnected(connected)
        })
    }, []);

    const handleSetVault = (id: string) => {
        setActiveVaultId(id)
    }

    const handleAddVault = () => {
        setEditingVault(null)
        setVaultDialogOpen(true)
    }

    const handleEditVault = (vault: VaultConfig) => {
        console.log(vault)
        setEditingVault(vault)
        setVaultDialogOpen(true)
    }

    const handleSaveVault = (vault: VaultConfig) => {
        if (editingVault) {
            setVaults(prev => prev.map(v => v.id === vault.id ? vault : v))
        } else {
            setVaults(prev => [...prev, vault])
            setActiveVaultId(vault.id)
        }
    }

    return (
        <div className="h-screen flex flex-col bg-background">
            <Header
                activeVaultId={activeVaultId}
                vaults={vaults}
                onSetVault={handleSetVault}
                onAddVault={handleAddVault}
                onEditVault={handleEditVault}
                onMinimize={() => window.electronAPI?.minimize()}
                onMaximize={() => window.electronAPI?.maximize()}
                onClose={() => window.electronAPI?.close()}
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
                isConnected={isConnected}
            />

            <VaultDialog
                open={vaultDialogOpen}
                onOpenChange={setVaultDialogOpen}
                onSave={handleSaveVault}
                editVault={editingVault}
            />
        </div>
    );
}
