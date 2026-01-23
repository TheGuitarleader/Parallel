import {useEffect, useRef, useState} from "react";
import { client } from "./../lib/messageClient";

// User List page - demonstrates working with arrays and lists
export function LogsPage() {
    const [logs, setLogs] = useState<string[]>([]);
    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const onLog = (log: string) => {
            setLogs(prev => [...prev.slice(-499), log]); // keep last 500
        };

        client.on("LogMessage", onLog);

        return () => {
            client.off("LogMessage", onLog);
        };
    }, []);

    return (
        <div className="space-y-8">
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <div className="space-y-2">
                    {logs.map((log, i) => (
                        <div
                            key={i}
                            className={
                                log.includes("ERROR") || log.includes("CRITICAL")
                                    ? "text-red-500"
                                    : log.includes("WARN")
                                        ? "text-yellow-500"
                                        : "text-foreground"
                            }
                        >
                            {log}
                        </div>
                    ))}

                    <div ref={bottomRef} />
                </div>
            </div>

            {/* Learning Notes */}
            <div className="bg-card border border-border rounded-lg p-6">
                <h3>What You're Learning</h3>
                <ul className="mt-4 space-y-2 text-muted-foreground">
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Forms:</strong> Using controlled inputs with onChange</span>
                    </li>
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Lists:</strong> Mapping arrays to render multiple items</span>
                    </li>
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Array Methods:</strong> Using filter() to remove items</span>
                    </li>
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Spread Operator:</strong> Using [...array, newItem] to add items</span>
                    </li>
                </ul>
            </div>
        </div>
    );
}