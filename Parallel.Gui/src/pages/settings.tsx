import { useState } from "react";

// Settings page - demonstrates various form controls
export function SettingsPage() {
    // Different states for different settings
    const [username, setUsername] = useState("ReactLearner");
    const [email, setEmail] = useState("user@example.com");
    const [darkMode, setDarkMode] = useState(true);
    const [notifications, setNotifications] = useState(false);
    const [theme, setTheme] = useState("blood-red");

    // Function to simulate saving settings
    const saveSettings = () => {
        alert("Settings saved! (This is just a demo)");
    };

    return (
        <div className="space-y-8">
            <div>
                <h1>Settings</h1>
                <p className="text-muted-foreground mt-2">
                    Manage your preferences and learn about different input types.
                </p>
            </div>

            {/* Profile Settings */}
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <h3>Profile Settings</h3>

                <div>
                    <label className="block text-sm mb-2">Username</label>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        className="w-full px-4 py-2 bg-input border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                </div>

                <div>
                    <label className="block text-sm mb-2">Email</label>
                    <input
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        className="w-full px-4 py-2 bg-input border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                </div>
            </div>

            {/* Appearance Settings */}
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <h3>Appearance</h3>

                <div>
                    <label className="block text-sm mb-2">Theme Color</label>
                    <div className="flex gap-2 flex-wrap">
                        {["blood-red", "blue", "green", "purple"].map((color) => (
                            <button
                                key={color}
                                onClick={() => setTheme(color)}
                                className={`px-4 py-2 rounded-lg transition-colors capitalize ${
                                    theme === color
                                        ? "bg-primary text-primary-foreground"
                                        : "bg-secondary text-secondary-foreground hover:bg-accent"
                                }`}
                            >
                                {color}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="flex items-center justify-between p-4 bg-accent rounded-lg">
                    <div>
                        <p className="font-medium">Dark Mode</p>
                        <p className="text-sm text-muted-foreground">
                            Use dark theme across the app
                        </p>
                    </div>
                    <label className="relative inline-block w-12 h-6">
                        <input
                            type="checkbox"
                            checked={darkMode}
                            onChange={(e) => setDarkMode(e.target.checked)}
                            className="sr-only peer"
                        />
                        <div className="w-12 h-6 bg-secondary rounded-full peer-checked:bg-primary transition-colors cursor-pointer">
                            <div
                                className={`absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform ${
                                    darkMode ? "translate-x-6" : ""
                                }`}
                            />
                        </div>
                    </label>
                </div>
            </div>

            {/* Notification Settings */}
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <h3>Notifications</h3>

                <div className="flex items-center justify-between p-4 bg-accent rounded-lg">
                    <div>
                        <p className="font-medium">Enable Notifications</p>
                        <p className="text-sm text-muted-foreground">
                            Receive updates and alerts
                        </p>
                    </div>
                    <label className="relative inline-block w-12 h-6">
                        <input
                            type="checkbox"
                            checked={notifications}
                            onChange={(e) => setNotifications(e.target.checked)}
                            className="sr-only peer"
                        />
                        <div className="w-12 h-6 bg-secondary rounded-full peer-checked:bg-primary transition-colors cursor-pointer">
                            <div
                                className={`absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform ${
                                    notifications ? "translate-x-6" : ""
                                }`}
                            />
                        </div>
                    </label>
                </div>
            </div>

            {/* Current Settings Display */}
            <div className="bg-card border border-border rounded-lg p-6">
                <h3 className="mb-4">Current Settings</h3>
                <div className="space-y-2 text-muted-foreground">
                    <p>
                        <strong>Username:</strong> {username}
                    </p>
                    <p>
                        <strong>Email:</strong> {email}
                    </p>
                    <p>
                        <strong>Theme:</strong> <span className="capitalize">{theme}</span>
                    </p>
                    <p>
                        <strong>Dark Mode:</strong> {darkMode ? "Enabled" : "Disabled"}
                    </p>
                    <p>
                        <strong>Notifications:</strong>{" "}
                        {notifications ? "Enabled" : "Disabled"}
                    </p>
                </div>
            </div>

            {/* Save Button */}
            <button
                onClick={saveSettings}
                className="w-full px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity"
            >
                Save Settings
            </button>
        </div>
    );
}