import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import BaselineDemo from './components/BaselineDemo';
import { AuthProvider } from './context/AuthContext';
import LoginPage from './pages/LoginPage';
import RegistrationPage from './pages/RegistrationPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<BaselineDemo />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegistrationPage />} />
          <Route path="/dashboard" element={<PageStub title="Patient Dashboard" />} />
          <Route path="/staff/queue" element={<PageStub title="Staff Queue" />} />
          <Route path="/admin/users" element={<PageStub title="Admin Users" />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

function PageStub({ title }: { title: string }) {
  return (
    <main>
      <h1>{title}</h1>
    </main>
  );
}

export default App;
