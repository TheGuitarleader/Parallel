import { useState } from "react";

// This is the Home page component
// It demonstrates basic state management with buttons
export function HomePage() {
    // useState is a React Hook that lets us add state to our component
    // It returns [currentValue, functionToUpdateValue]
    const [message, setMessage] = useState("Click a button to change this message!");
    const [isVisible, setIsVisible] = useState(true);

    return (
        <div className="space-y-8">
            <div>
                <h1>Welcome to React! 👋</h1>
                <p className="text-muted-foreground mt-2">
                    This is a beginner-friendly dashboard to learn React basics.
                </p>
            </div>

            {/* Card 1: Button State */}
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <h3>Button State Example</h3>
                <p className="text-muted-foreground">
                    Click these buttons to change the message below:
                </p>

                <div className="flex gap-3 flex-wrap">
                    <button
                        onClick={() => setMessage("Hello from React!")}
                        className="px-4 py-2 bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity"
                    >
                        Say Hello
                    </button>
                    <button
                        onClick={() => setMessage("You're learning React state!")}
                        className="px-4 py-2 bg-secondary text-secondary-foreground rounded-lg hover:bg-accent transition-colors"
                    >
                        Show State Info
                    </button>
                    <button
                        onClick={() => setMessage("Great job! 🎉")}
                        className="px-4 py-2 bg-secondary text-secondary-foreground rounded-lg hover:bg-accent transition-colors"
                    >
                        Celebrate
                    </button>
                </div>

                {/* This is the message that changes based on which button is clicked */}
                <div className="p-4 bg-accent rounded-lg">
                    <p className="text-center">{message}</p>
                </div>
            </div>

            {/* Card 2: Toggle Visibility */}
            <div className="bg-card border border-border rounded-lg p-6 space-y-4">
                <h3>Toggle Visibility Example</h3>
                <p className="text-muted-foreground">
                    This demonstrates conditional rendering in React:
                </p>

                <button
                    onClick={() => setIsVisible(!isVisible)}
                    className="px-4 py-2 bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity"
                >
                    {isVisible ? "Hide" : "Show"} Content
                </button>

                {/* Conditional rendering: only show this div if isVisible is true */}
                {isVisible && (
                    <div className="p-4 bg-accent rounded-lg border-l-4 border-primary">
                        <p>🎯 This content can be toggled on and off!</p>
                        <p className="text-sm text-muted-foreground mt-2">
                            In React, we use conditional rendering (like {`{isVisible && <div>...</div>}`})
                            to show or hide elements.
                        </p>
                    </div>
                )}
            </div>

            {/* Info Card */}
            <div className="bg-card border border-border rounded-lg p-6">
                <h3>What You're Learning</h3>
                <ul className="mt-4 space-y-2 text-muted-foreground">
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>State:</strong> Using useState to store and update data</span>
                    </li>
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Events:</strong> Handling button clicks with onClick</span>
                    </li>
                    <li className="flex items-start gap-2">
                        <span className="text-primary">•</span>
                        <span><strong>Conditional Rendering:</strong> Showing/hiding content based on state</span>
                    </li>
                </ul>
            </div>
        </div>
    );
}