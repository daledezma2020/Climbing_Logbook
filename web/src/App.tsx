import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import Layout from '@/components/layout/Layout';
import Home from '@/pages/Home';
import Logbook from '@/pages/Logbook';
import Climbs from '@/pages/Climbs';
import LogClimb from '@/pages/LogClimb';
import Profile from '@/pages/Profile';
import EditProfile from '@/pages/EditProfile';
import Users from '@/pages/Users';
import UserConnections from '@/pages/UserConnections';

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
        path: 'users',
        element: <Users />,
      },
      {
        path: 'users/:username',
        element: <Profile />,
      },
      {
        path: 'users/:username/followers',
        element: <UserConnections kind="followers" />,
      },
      {
        path: 'users/:username/following',
        element: <UserConnections kind="following" />,
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
