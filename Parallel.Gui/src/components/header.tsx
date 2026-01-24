import {Plus, Pencil, Database, Minus, Square, X} from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {type Vault, vaultServiceIcons} from "@/lib/types.ts";

interface HeaderProps {
    activeVaultId: string | null
    vaults: Vault[]
    onSetVault: (id: string) => void
    onClose: () => void
    onMinimize: () => void
    onMaximize: () => void
}

export function Header({
    activeVaultId,
    vaults,
    onSetVault,
    onClose,
    onMinimize,
    onMaximize,
}: HeaderProps) {
    const onEditVault = (vault: Vault) => {
        console.log("Edit vault:", vault)
    }

    const onAddVault = () => {
        console.log("Add vault")
    }

    return (
        <header className="bg-card border-b border-border">
            {/* Window control */}
            <div className="h-12 flex items-center px-4 pr-2 gap-4 -webkit-app-region: drag">
                {/* Logo */}
                <div className="flex items-center gap-3">
                    <img src="/src/assets/parallel.svg" className="w-6 h-6" alt="Parallel"/>
                </div>

                <div className="flex-5 flex justify-center">
                    <span className="text-sm text-primary font-medium">Parallel Desktop</span>
                </div>

                <div className="flex items-center gap-1 ml-4 -webkit-app-region: no-drag">
                    <button
                        onClick={onMinimize}
                        className="w-8 h-8 flex items-center justify-center hover:bg-gray-300/20 rounded"
                    >
                        <Minus className="w-4 h-4" />
                    </button>
                    <button
                        onClick={onMaximize}
                        className="w-8 h-8 flex items-center justify-center hover:bg-gray-300/20 rounded"
                    >
                        <Square className="w-4 h-4" />
                    </button>
                    <button
                        onClick={onClose}
                        className="w-8 h-8 flex items-center justify-center hover:bg-red-500 rounded"
                    >
                        <X className="w-5 h-5" />
                    </button>
                </div>
            </div>


            {/* Vault tabs */}
            <div className="flex items-center gap-1 px-2 pb-2 overflow-x-auto">
                {vaults.map(vault => {
                    const Icon = vaultServiceIcons[vault.credentials.service] ?? Database
                    const isActive = vault.id === activeVaultId

                    return (
                        <div
                            key={vault.id}
                            className={cn(
                                "group relative flex items-center gap-2 px-3 py-1.5 rounded-md text-sm transition-all cursor-pointer",
                                isActive
                                    ? "bg-primary text-primary-foreground"
                                    : "bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground",
                                !vault.enabled && "opacity-50"
                            )}
                            onClick={() => onSetVault(vault.id)}
                        >
                            <Icon className="w-3.5 h-3.5" />
                            <span className="max-w-[120px] truncate">{vault.name}</span>

                            {/* Edit button on hover */}
                            <button
                                onClick={(e) => {
                                    e.stopPropagation()
                                    onEditVault(vault)
                                }}
                                className={cn(
                                    "opacity-0 group-hover:opacity-100 transition-opacity ml-1 p-0.5 rounded hover:bg-background/20",
                                    isActive && "hover:bg-primary-foreground/20"
                                )}
                            >
                                <Pencil className="w-3 h-3" />
                            </button>
                        </div>
                    )
                })}

                {/* Add Vault Button */}
                <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 gap-1 text-muted-foreground hover:text-foreground"
                    onClick={onAddVault}
                >
                    <Plus className="w-4 h-4" />
                    <span className="text-xs">Add Vault</span>
                </Button>
            </div>
        </header>
    )
}