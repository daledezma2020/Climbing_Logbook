import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import Layout from '@/components/layout/Layout';
import Home from '@/pages/Home';
import Logbook from '@/pages/Logbook';
import Climbs from '@/pages/Climbs';
import LogClimb from '@/pages/LogClimb';

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Home />,
      },
      {
        path: 'logbook',
        element: <Logbook />,
      },
      {
        path: 'climbs',
        element: <Climbs />,
      },
      {
        path: 'log/new',
        element: <LogClimb />,
      },
    ],
  },
]);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
