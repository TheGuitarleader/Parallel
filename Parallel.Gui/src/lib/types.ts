import {Cloud, HardDrive, Server} from "lucide-react";

export type Vault = {
    id: string
    name: string
    enabled: boolean
    credentials: {
        service: string
        rootDirectory: string | null
        address: string | null
        username: string | null
        password: string | null
        encrypt: boolean
        encryptionKey: string | null
        forceStyle: boolean
    }
}

export const vaultServiceIcons: Record<string, React.ElementType> = {
    Local: HardDrive,
    SSH: Server,
    S3: Cloud
}