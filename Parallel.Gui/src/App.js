import React, { useState } from 'react';

export default function App() {
  const [message, setMessage] = useState('Hello from React + Electron!');

  const handleClick = () => {
    setMessage('Button clicked!');
  };

  return (
    <div style={{ padding: '2rem', fontFamily: 'sans-serif' }}>
      <h1>{message}</h1>
      <button onClick={handleClick} style={{ padding: '0.5rem 1rem' }}>
        Click Me
      </button>
    </div>
  );
}