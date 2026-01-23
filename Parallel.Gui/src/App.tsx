import { useState } from "react";
import { Sidebar } from "./components/sidebar.tsx";
import { Footer } from "./components/footer.tsx";
import { HomePage } from "./pages/home.tsx";
import { LogsPage } from "./pages/logging.tsx";
import { ExplorerPage } from "./pages/explorer.tsx";
import { SettingsPage } from "./pages/settings.tsx";

// Main App component - this is the root of our application
export default function App() {
    // State to track which page we're currently on
    const [currentPage, setCurrentPage] = useState("home");

    // Function to render the correct page based on currentPage state
    const renderPage = () => {
        switch (currentPage) {
            case "home":
                return <HomePage />;
            case "explorer":
                return <ExplorerPage />;
            case "logging":
                return <LogsPage />;
            case "settings":
                return <SettingsPage />;
            default:
                return <HomePage />;
        }
    };

    return (
        // Apply dark mode class to the entire app
        <div className="dark h-screen w-screen flex bg-background text-foreground overflow-hidden">
            {/* Sidebar - we pass currentPage and setCurrentPage as props */}
            <Sidebar currentPage={currentPage} onPageChange={setCurrentPage} />

            {/* Main content area with footer */}
            <div className="flex-1 flex flex-col overflow-hidden">
                <main className="flex-1 overflow-y-auto">
                    <div className="max-w-5xl mx-auto p-8">
                        {/* This renders the current page */}
                        {renderPage()}
                    </div>
                </main>

                {/* Footer only across the page content */}
                <Footer />
            </div>
        </div>
    );
}
