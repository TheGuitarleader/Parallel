import {Cloud, HardDrive, Server} from "lucide-react";

export type VaultType = "local" | "remote" | "cloud"
export type CloudProvider = "aws" | "storj" | "wasabi" | "custom"

export type VaultConfig = {
    id: string
    name: string
    enabled: boolean
    credentials: {
        service: VaultType
        rootDirectory: string | null
        address?: string | null
        username?: string | null
        password?: string | null
        encrypt: boolean | true
        encryptionKey?: string | null
        cloudProvider?: CloudProvider
    },
}

export type VaultStats = {
    lastSync: string
    localSize: number
    remoteSize: number
    totalSize: number
    totalFiles: number
    totalLocalFiles: number
    totalDeletedFiles: number
}

export const vaultServiceIcons: Record<string, React.ElementType> = {
    Local: HardDrive,
    Remote: Server,
    Cloud: Cloud
}