import {Home, TextSearchIcon, Settings, FolderOpen} from "lucide-react";

// This is a simple Sidebar component that shows navigation links
// In React, we pass "props" (properties) to components to customize them
interface SidebarProps {
    currentPage: string;
    onPageChange: (page: string) => void;
}

export function Sidebar({currentPage, onPageChange}: SidebarProps) {
    const mainMenuItems = [
        {id: "home", label: "Home", icon: Home},
        {id: "explorer", label: "Explorer", icon: FolderOpen},
        {id: "logging", label: "Logs", icon: TextSearchIcon},
    ];

    const settingsItem = {id: "settings", label: "Settings", icon: Settings};

    return (
        <div className="w-20 h-full bg-sidebar border-r border-border flex flex-col items-center">
            <div className="py-6 border-b border-border w-full flex justify-center">
                <img src="/src/assets/parallel.svg" className="w-10 h-10" alt="Parallel"/>
            </div>

            <nav className="flex-1 py-4 flex flex-col gap-2 w-full px-3">
                {/* .map() is used to loop through arrays in React */}
                {mainMenuItems.map((item) => {
                    const Icon = item.icon;
                    const isActive = currentPage === item.id;

                    return (
                        <button
                            key={item.id} // Every item in a list needs a unique "key"
                            onClick={() => onPageChange(item.id)} // When clicked, change the page
                            title={item.label} // Tooltip on hover
                            className={`w-full h-14 flex items-center justify-center rounded-lg transition-colors duration-200
                            ${ isActive ? "bg-primary text-primary-foreground" : "hover:bg-accent text-foreground" }`}>
                            <Icon className="w-6 h-6"/>
                        </button>
                    );
                })}
            </nav>

            {/* Settings at Bottom */}
            <div className="py-4 border-t border-border w-full px-3">
                <button
                    onClick={() => onPageChange(settingsItem.id)}
                    title={settingsItem.label}
                    className={`
            w-full h-14 flex items-center justify-center rounded-lg
            transition-colors duration-200
            ${
                        currentPage === settingsItem.id
                            ? "bg-primary text-primary-foreground"
                            : "hover:bg-accent text-foreground"
                    }
          `}
                >
                    <Settings className="w-6 h-6"/>
                </button>
            </div>
        </div>
    );
}
