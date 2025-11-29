import { useEffect, useState } from 'react'
import './App.css'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

function App() {
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    fetch('http://localhost:5050/weatherforecast')
      .then(res => res.json())
      .then(data => setData(data))
      .catch(err => console.error(err));
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-5xl font-bold text-slate-900 tracking-tight">
            Climbing Logbook
          </h1>
          <p className="text-slate-600">Track your climbing journey</p>
        </div>

        <Card className="shadow-lg">
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>API Connection Test</span>
              <Badge variant={data ? "default" : "secondary"}>
                {data ? "Connected" : "Loading..."}
              </Badge>
            </CardTitle>
            <CardDescription>
              Testing connection to .NET backend
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="bg-slate-50 p-4 rounded-lg">
              <pre className="text-sm overflow-auto">
                {data ? JSON.stringify(data, null, 2) : 'Fetching data...'}
              </pre>
            </div>
            <div className="flex gap-2">
              <Button onClick={() => window.location.reload()}>
                Refresh Data
              </Button>
              <Button variant="outline">
                View Details
              </Button>
            </div>
          </CardContent>
        </Card>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Total Climbs</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">0</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Hardest Grade</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">-</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">This Month</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">0</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

export default App