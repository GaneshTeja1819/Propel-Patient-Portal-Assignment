import { BrowserRouter, Routes, Route } from 'react-router-dom';
import BaselineDemo from './components/BaselineDemo';
import { IntakePage } from './pages/IntakePage';
import { ProtectedRoute } from './components/auth/ProtectedRoute';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/intake/:appointmentId"
          element={
            <ProtectedRoute>
              <IntakePage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<main><BaselineDemo /></main>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
