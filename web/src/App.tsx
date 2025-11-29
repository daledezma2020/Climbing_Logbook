import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    fetch('http://localhost:5050/weatherforecast')
      .then(res => res.json())
      .then(data => setData(data))
      .catch(err => console.error(err));
  }, []);

  return (
    <div>
      <h1>React + .NET App</h1>
      <pre>{JSON.stringify(data, null, 2)}</pre>
    </div>
  )
}

export default App