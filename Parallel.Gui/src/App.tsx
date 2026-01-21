import React from 'react'
import MyButton from './components/MyButton'

function App() {
  const handleClick = () => {
    alert('Hello from Electron + React!')
  }

  return (
    <div style={{ padding: '50px', textAlign: 'center' }}>
      <h1>Parallel GUI</h1>
      <MyButton onClick={handleClick} />
    </div>
  )
}

export default App
