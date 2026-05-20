import { BrowserRouter, Routes, Route } from 'react-router-dom';
import BaselineDemo from './components/BaselineDemo';
import { IntakePage } from './pages/IntakePage';
import { DocumentUploadPage } from './pages/DocumentUploadPage';
import { PatientProfilePage } from './pages/PatientProfilePage';
import { StaffPatientProfilePage } from './pages/StaffPatientProfilePage';
import { CodeVerificationPage } from './pages/CodeVerificationPage';
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
        <Route
          path="/documents/upload"
          element={
            <ProtectedRoute>
              <DocumentUploadPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/profile"
          element={
            <ProtectedRoute>
              <PatientProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff/patients/:id"
          element={
            <ProtectedRoute>
              <StaffPatientProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff/coding/:encounterId"
          element={
            <ProtectedRoute>
              <CodeVerificationPage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<main><BaselineDemo /></main>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
