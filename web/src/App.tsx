import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import Layout from '@/components/layout/Layout';
import Home from '@/pages/Home';
import Routes from '@/pages/Routes';
import CreateRoute from '@/pages/CreateRoute';
import EditRoute from '@/pages/EditRoute';
import { useParams } from 'react-router-dom';

const EditRouteWrapper = () => {
  const { id } = useParams<{ id: string }>();
  return <EditRoute id={parseInt(id || '0', 10)} />;
};

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
        path: 'routes',
        element: <Routes />,
      },
      {
        path: 'routes/create',
        element: <CreateRoute />,
      },
      {
        path: 'routes/edit/:id',
        element: <EditRouteWrapper />,
      }
    ],
  },
]);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
