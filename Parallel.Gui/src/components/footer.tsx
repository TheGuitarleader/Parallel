import { useState, useEffect } from "react";
import { Tooltip, TooltipContent, TooltipTrigger} from './ui/tooltip.tsx';
import {CheckCircle2, Clock, HardDrive, History, Trash2, Wifi, WifiOff} from "lucide-react";
import { client } from '../lib/messageClient.ts'
import {cn} from "../lib/utils.ts";
import {API_URL} from "@/lib/config.ts";
import type {VaultStats} from "@/lib/types.ts";
import {formatBytes, formatNum} from "@/lib/convert.ts";

interface StatusBarProps {
    activeVaultId: string;
}

// Footer component that displays status information
export function Footer({ activeVaultId } : StatusBarProps) {
    const [isConnected, setIsConnected] = useState(false);
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

        client.connectionChanged((connected) => setIsConnected(connected))
    }, []);

    const [syncedFiles, setSyncedFiles] = useState("0");
    const [revisedFiles, setRevisedFiles] = useState("0");
    const [deletedFiles, setDeletedFiles] = useState("0");
    const [storageSize, setStorageSize] = useState("0 Bytes");
    const [timestamp, setTimestamp] = useState("Never");
    useEffect(() => {
        (async () => {
            const res = await fetch(`${API_URL}/vaults/${activeVaultId}/stats`, { credentials: "include" })
            if (res.ok) {
                const data: VaultStats = await res.json()
                const revisions = data.totalFiles - (data.totalLocalFiles + data.totalDeletedFiles)

                setSyncedFiles(formatNum(data.totalLocalFiles))
                setRevisedFiles(formatNum(revisions))
                setDeletedFiles(formatNum(data.totalDeletedFiles))
                setStorageSize(formatBytes(data.localSize))
            }
            else {
                setSyncedFiles("0")
                setRevisedFiles("0")
                setDeletedFiles("0")
                setStorageSize(formatBytes(0))
                setTimestamp("Never")
            }
        })()
    }, [activeVaultId]);

    return (
        <footer className="h-10 bg-sidebar border-t border-border flex items-center justify-between px-4 text-sm">
            <div className={cn(
                "flex items-center gap-2 cursor-default",
                isConnected ? "text-status-synced" : "text-status-deleted"
            )}>
                <Tooltip>
                    <TooltipTrigger asChild>
                        <div className="pl-0 pr-1">
                            {isConnected ? (
                                <Wifi className="w-4.5 h-4.5" />
                            ) : (
                                <WifiOff className="w-4.5 h-4.5" />
                            )}
                        </div>
                    </TooltipTrigger>
                    <TooltipContent side="top">
                        <span>{isConnected ? "Connected to" : "Disconnected from"} service</span>
                    </TooltipContent>
                </Tooltip>

                <div className="h-4 w-px bg-border" />

                <div className="flex px-1 items-center gap-6">
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <div className="flex items-center gap-1.5 text-muted-foreground">
                                <CheckCircle2 className="w-3.5 h-3.5 text-status-synced" />
                                <span>{syncedFiles}</span>
                            </div>
                        </TooltipTrigger>
                        <TooltipContent side="top">
                            <p>Total synced files</p>
                        </TooltipContent>
                    </Tooltip>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <div className="flex items-center gap-1.5 text-muted-foreground">
                                <History className="w-3.5 h-3.5 text-status-pending" />
                                <span>{revisedFiles}</span>
                            </div>
                        </TooltipTrigger>
                        <TooltipContent side="top">
                            <p>Total file revisions</p>
                        </TooltipContent>
                    </Tooltip>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <div className="flex items-center gap-1.5 text-muted-foreground">
                                <Trash2 className="w-3.5 h-3.5 text-status-deleted" />
                                <span>{deletedFiles}</span>
                            </div>
                        </TooltipTrigger>
                        <TooltipContent side="top">
                            <p>Total deleted files</p>
                        </TooltipContent>
                    </Tooltip>
                </div>
            </div>

            <div className="flex items-center gap-6 text-muted-foreground">
                <Tooltip>
                    <TooltipTrigger asChild>
                        <div className="flex items-center gap-1.5 cursor-pointer">
                            <HardDrive className="w-3.5 h-3.5" />
                            <span>{storageSize}</span>
                        </div>
                    </TooltipTrigger>
                    <TooltipContent side="top">
                        <p>Total storage size</p>
                    </TooltipContent>
                </Tooltip>
                <Tooltip>
                    <TooltipTrigger asChild>
                        <div className="flex items-center gap-1.5 cursor-pointer">
                            <Clock className="w-3.5 h-3.5" />
                            <span>{timestamp}</span>
                        </div>
                    </TooltipTrigger>
                    <TooltipContent side="top">
                        <p>Last sync time</p>
                    </TooltipContent>
                </Tooltip>
            </div>
        </footer>
    );
}