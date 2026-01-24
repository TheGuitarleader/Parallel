import { useState, useEffect } from "react";
import { Tooltip, TooltipContent, TooltipTrigger} from './ui/tooltip.tsx';
import {Clock, FolderSync, HardDrive, Wifi, WifiOff} from "lucide-react";
import { client } from '../lib/messageClient.ts'
import {cn} from "../lib/utils.ts";

// StatusBar component that displays status information
export function StatusBar() {
    // State for connection status - toggles between connected/offline
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

    // State change for total storage size
    const [storageSize, setStorageSize] = useState("0 Bytes");
    useEffect(() => {
        const updateStorageSize = (size: string) => {
            setStorageSize(size)
        }

        client.on("StorageSizeUpdate", updateStorageSize)
        return () => {
            client.off("StorageSizeUpdate", updateStorageSize)
        }
    }, [])

    // State change for the last sync time
    const [timestamp, setTimestamp] = useState("Never");
    useEffect(() => {
        const updateLastSync = (time: string) => {
            console.log(time)

            setTimestamp(time)
        }

        client.on("LastSyncUpdate", updateLastSync)
        return () => {
            client.off("LastSyncUpdate", updateLastSync)
        }
    }, [])

    return (
        <footer className="h-10 bg-sidebar border-t border-border flex items-center justify-between px-4 text-sm">
            <div className={cn(
                "flex items-center gap-2 cursor-default",
                isConnected ? "text-status-synced" : "text-status-deleted"
            )}>
                {isConnected ? (
                    <Wifi className="w-4.5 h-4.5" />
                ) : (
                    <WifiOff className="w-4.5 h-4.5" />
                )}
                <span className="hidden sm:inline">
                  {isConnected ? "Connected" : "Disconnected"}
                </span>
            </div>

            <div className="flex items-center gap-6 text-muted-foreground">
                <Tooltip>
                    <TooltipTrigger asChild>
                        <div className="flex items-center gap-1.5 cursor-pointer">
                            <FolderSync className="w-3.5 h-3.5" />
                            <span>123,456</span>
                        </div>
                    </TooltipTrigger>
                    <TooltipContent side="top">
                        <p>Total files synced</p>
                    </TooltipContent>
                </Tooltip>
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