import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import Layout from '@/components/layout/Layout';
import Home from '@/pages/Home';
import Logbook from '@/pages/Logbook';
import Climbs from '@/pages/Climbs';
import LogClimb from '@/pages/LogClimb';
import Profile from '@/pages/Profile';
import EditProfile from '@/pages/EditProfile';

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
      {
        path: 'users/:username',
        element: <Profile />,
      },
      {
        path: 'profile/edit',
        element: <EditProfile />,
      },
    ],
  },
]);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
