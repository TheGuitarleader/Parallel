import React from 'react'

type Props = {
  onClick: () => void
}

const MyButton: React.FC<Props> = ({ onClick }) => {
  return (
    <button
      style={{
        padding: '10px 20px',
        fontSize: '16px',
        borderRadius: '5px',
        cursor: 'pointer',
      }}
      onClick={onClick}
    >
      Click Me
    </button>
  )
}

export default MyButton
