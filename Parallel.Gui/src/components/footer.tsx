import { useState, useEffect } from "react";
import { Tooltip, TooltipContent, TooltipTrigger} from './ui/tooltip.tsx';
import {Clock, HardDrive, Wifi, WifiOff} from "lucide-react";
import { client } from '../lib/messageClient.ts'

// Footer component that displays status information
export function Footer() {
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

        client.on("TotalStorageUpdate", updateStorageSize)
        return () => {
            client.off("TotalStorageUpdate", updateStorageSize)
        }
    }, [])

    // State change for the last sync time
    const [timestamp, setTimestamp] = useState("Not synced yet");
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
            {/* Left side - Connection Status */}
            <div className="flex items-center gap-2">
                <button className="flex items-center gap-2 hover:opacity-80 transition-opacity" onClick={async () => { await client.connect() }}>
                    {isConnected ? (
                        <Wifi className="w-4 h-4 text-green-500" />
                    ) : (
                        <WifiOff className="w-4 h-4 text-red-500" />
                    )}

                    <span className={isConnected ? "text-green-500" : "text-red-500"}>
                        {isConnected ? "Connected" : "Offline"}
                    </span>
                </button>
            </div>

            {/* Right side - Storage and Timestamp */}
            <div className="flex items-center gap-6 text-muted-foreground">
                <Tooltip>
                    <TooltipTrigger asChild>
                        <div className="flex items-center gap-2 cursor-help">
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
                        <div className="flex items-center gap-2 cursor-help">
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