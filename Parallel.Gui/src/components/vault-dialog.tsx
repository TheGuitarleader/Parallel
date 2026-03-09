import {useEffect, useState} from "react";
import type {CloudProvider, VaultConfig, VaultType} from "@/lib/types.ts";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from "@/components/ui/dialog.tsx";
import {Cloud, Eye, EyeOff, HardDrive, RefreshCw, Server} from "lucide-react";
import { Label } from "./ui/label";
import { Button } from "./ui/button";
import {cn, generateHash, getMachineName} from "@/lib/utils";
import { Input } from "./ui/input";
import {Switch} from "@/components/ui/switch.tsx";
import { Separator } from "./ui/separator";
import { Tabs, TabsList, TabsTrigger } from "./ui/tabs";

interface VaultDialogProps {
    open: boolean
    onOpenChange: (open: boolean) => void
    onSave: (vault: VaultConfig) => void
    editVault?: VaultConfig | null
}

const cloudProviders: { value: CloudProvider; label: string; endpoint: string }[] = [
    { value: "aws", label: "Amazon S3", endpoint: "s3.amazonaws.com" },
    { value: "storj", label: "Storj", endpoint: "gateway.storjshare.io" },
    { value: "wasabi", label: "Wasabi", endpoint: "s3.wasabisys.com" },
    { value: "custom", label: "Custom S3", endpoint: "" },
]

export function VaultDialog({ open, onOpenChange, onSave, editVault }: VaultDialogProps) {
    const [showPassword, setShowPassword] = useState(false)
    const [showSecretKey, setShowSecretKey] = useState(false)

    const [formData, setFormData] = useState<VaultConfig>({
        id: generateHash(8),
        name: "Default",
        enabled: true,
        credentials: {
            service: "remote",
            rootDirectory: '',
            address: null,
            username: null,
            password: null,
            encrypt: false,
            encryptionKey: null,
            cloudProvider: "aws"
        }
    })

    useEffect(() => {
        Promise.resolve().then(async () => {
            if (editVault) {
                setFormData(editVault)
            } else {
                setFormData({
                    id: generateHash(8),
                    name: await getMachineName(),
                    enabled: true,
                    credentials: {
                        service: "local",
                        rootDirectory: null,
                        address: null,
                        username: null,
                        password: null,
                        encrypt: false,
                        encryptionKey: null,
                        cloudProvider: "aws",
                    }
                })
            }
        })
    }, [editVault, open])


    const handleTypeChange = (type: VaultType) => {
        setFormData(prev => ({
            ...prev,
            credentials: {
                ...prev.credentials,
                service: type,
                rootDirectory: prev.credentials?.rootDirectory ?? "",
                address: null,
                username: null,
                password: null,
                encrypt: prev.credentials?.encrypt ?? false, // ensure required field
                cloudProvider: type === "cloud" ? "aws" : undefined,
            },
        }));
    };


    const handleCloudProviderChange = (provider: CloudProvider) => {
        const providerConfig = cloudProviders.find(p => p.value === provider)
        setFormData(prev => ({
            ...prev,
            cloudProvider: provider,
            address: providerConfig?.endpoint || "",
        }))
    }

    const handleSave = () => {
        onSave(formData as VaultConfig)
        onOpenChange(false)
    }

    const regenerateId = () => {
        setFormData(prev => ({ ...prev, id: generateHash(8) }))
    }

    const isValid = formData.name && formData.credentials?.rootDirectory && formData.id

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[500px] bg-card">
                <DialogHeader>
                    <DialogTitle>{editVault ? "Edit Vault" : "Create New Vault"}</DialogTitle>
                    <DialogDescription>
                        Configure a storage location for your files.
                    </DialogDescription>
                </DialogHeader>

                <div className="space-y-6 py-4">
                    <div className="space-y-3">
                        <Label>Vault Type</Label>
                        <div className="grid grid-cols-3 gap-2">
                            {[
                                { type: "local" as VaultType, icon: HardDrive, label: "Local" },
                                { type: "remote" as VaultType, icon: Server, label: "Remote" },
                                { type: "cloud" as VaultType, icon: Cloud, label: "Cloud" },
                            ].map(({ type, icon: Icon, label }) => (
                                <button
                                    key={type}
                                    type="button"
                                    onClick={() => handleTypeChange(type)}
                                    className={cn(
                                        "flex flex-col items-center gap-2 p-4 rounded-lg border-2 transition-all",
                                        formData.credentials?.service === type
                                            ? "border-primary bg-primary/10"
                                            : "border-border hover:border-muted-foreground/50 bg-transparent"
                                    )}
                                >
                                    <Icon className={cn(
                                        "w-6 h-6",
                                        formData.credentials?.service === type ? "text-primary" : "text-muted-foreground"
                                    )} />
                                    <span className={cn(
                                        "text-sm font-medium",
                                        formData.credentials?.service === type ? "text-foreground" : "text-muted-foreground"
                                    )}>{label}</span>
                                </button>
                            ))}
                        </div>
                    </div>
                </div>

                <div className="grid gap-4">
                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="name">Vault Name</Label>
                            <Input
                                id="name"
                                value={formData.name || ""}
                                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                                placeholder="My Vault"
                            />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="id">Vault ID</Label>
                            <div className="flex gap-2">
                                <Input
                                    id="id"
                                    value={formData.id || ""}
                                    onChange={(e) => setFormData(prev => ({ ...prev, id: e.target.value }))}
                                    placeholder="xxxxxxxx"
                                    className="font-mono text-sm"
                                />
                                <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    onClick={regenerateId}
                                    title="Generate new ID"
                                    className="shrink-0 bg-transparent"
                                >
                                    <RefreshCw className="w-4 h-4" />
                                </Button>
                            </div>
                        </div>
                    </div>

                    <div className="space-y-2">
                        <Label htmlFor="rootFolder">{
                            formData.credentials?.service === "cloud" ? "Bucket Name" : "Root Folder"
                        }</Label>
                        <Input
                            id="rootFolder"
                            value={formData.credentials?.rootDirectory || ""}
                            placeholder={formData.credentials?.service === "local" ? "C:\\Users" : "/home" }
                            onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, rootDirectory: e.target.value } }))}
                        />
                        <p className="text-xs text-muted-foreground">
                            The base directory where backup data will be stored.
                        </p>
                    </div>

                    <div className="flex items-center justify-between">
                        <div className="space-y-0.5">
                            <Label htmlFor="enabled">Enabled</Label>
                            <p className="text-xs text-muted-foreground">
                                Enable or disable this vault for syncing.
                            </p>
                        </div>
                        <div>
                            <Switch
                                id="enabled"
                                checked={formData.enabled}
                                onCheckedChange={(checked) => setFormData(prev => ({ ...prev, enabled: checked }))}
                            />
                        </div>
                    </div>

                    {formData.credentials?.service === "remote" && (
                        <>
                            <Separator />
                            <div className="space-y-4">
                                <h4 className="text-sm font-medium">Remote Connection</h4>
                                <div className="space-y-2">
                                    <Label htmlFor="address">Server Address</Label>
                                    <Input
                                        id="address"
                                        value={formData.credentials?.address || ""}
                                        placeholder="192.168.1.100 or hostname.local"
                                        onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, address: e.target.value } }))}
                                    />
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="username">Username</Label>
                                        <Input
                                            id="username"
                                            value={formData.credentials?.username || ""}
                                            onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, username: e.target.value } }))}
                                            placeholder="admin"
                                        />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="password">Password</Label>
                                        <div className="relative">
                                            <Input
                                                id="password"
                                                type={showPassword ? "text" : "password"}
                                                value={formData.credentials?.password || ""}
                                                onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, password: e.target.value } }))}
                                                placeholder="••••••••"
                                                className="pr-10"
                                            />
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                                                onClick={() => setShowPassword(!showPassword)}
                                            >
                                                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </>
                    )}

                    {formData.credentials?.service === "cloud" && (
                        <>
                            <Separator />
                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <h4 className="text-sm font-medium">S3-Compatible Storage</h4>
                                </div>

                                <Tabs value={formData.credentials?.cloudProvider} onValueChange={(type) => handleCloudProviderChange(type as CloudProvider)}>
                                    <TabsList className="grid w-full grid-cols-4">
                                        <TabsTrigger value="aws">AWS</TabsTrigger>
                                        <TabsTrigger value="storj">Storj</TabsTrigger>
                                        <TabsTrigger value="wasabi">Wasabi</TabsTrigger>
                                        <TabsTrigger value="custom">Custom</TabsTrigger>
                                    </TabsList>
                                </Tabs>

                                <div className="space-y-2">
                                    <Label htmlFor="endpoint">Endpoint</Label>
                                    <Input
                                        id="endpoint"
                                        value={formData.credentials?.address || ""}
                                        onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, address: e.target.value } }))}
                                        placeholder="s3.amazonaws.com"
                                    />
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="accessKey">Access Key</Label>
                                        <Input
                                            id="accessKey"
                                            value={formData.credentials?.username || ""}
                                            onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, username: e.target.value } }))}
                                            placeholder="AKIAIOSFODNN7EXAMPLE"
                                            className="font-mono text-sm"
                                        />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="secretKey">Secret Key</Label>
                                        <div className="relative">
                                            <Input
                                                id="secretKey"
                                                type={showSecretKey ? "text" : "password"}
                                                value={formData.credentials?.password || ""}
                                                onChange={(e) => setFormData(prev => ({ ...prev, credentials: { ...prev.credentials, password: e.target.value } }))}
                                                placeholder="••••••••••••••••••••"
                                                className="pr-10 font-mono text-sm"
                                            />
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                                                onClick={() => setShowSecretKey(!showSecretKey)}
                                            >
                                                {showSecretKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </>
                    )}
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent">
                        Cancel
                    </Button>
                    <Button onClick={handleSave} disabled={!isValid}>
                        {editVault ? "Save Changes" : "Create Vault"}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}