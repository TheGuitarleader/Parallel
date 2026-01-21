import React, { useState } from 'react';
import MyButton from './components/MyButton'

function App() {
  const [message, setMessage] = useState('');

  const handleClick = async () => {
    try {
      const message = await window.electronAPI.sayHello("Electron User");
      console.log(message); // should print: "Hello Electron User"
    } catch (err) {
      console.error("IPC error:", err);
    }
  };
  
  return (
    <div style={{ padding: '50px', textAlign: 'center' }}>
      <h1>Parallel GUI</h1>
      <MyButton onClick={handleClick} />
    </div>
  )
}

export default App
