import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import BaselineDemo from './components/BaselineDemo';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { IntakePage } from './pages/IntakePage';
import { DocumentUploadPage } from './pages/DocumentUploadPage';
import { PatientProfilePage } from './pages/PatientProfilePage';
import { StaffPatientProfilePage } from './pages/StaffPatientProfilePage';
import { CodeVerificationPage } from './pages/CodeVerificationPage';
import AdminAuditLogPage from './pages/AdminAuditLogPage';
import AdminUserManagementPage from './pages/AdminUserManagementPage';
import AppointmentDetailPage from './pages/AppointmentDetailPage';
import BookingPage from './pages/BookingPage';
import CalendarSyncPage from './pages/CalendarSyncPage';
import DashboardPage from './pages/DashboardPage';
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
          <Route path="/intake/:appointmentId" element={<ProtectedRoute><IntakePage /></ProtectedRoute>} />
          <Route path="/documents/upload" element={<ProtectedRoute><DocumentUploadPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><PatientProfilePage /></ProtectedRoute>} />
          <Route path="/booking" element={<BookingPage />} />
          <Route path="/calendar-sync" element={<ProtectedRoute><CalendarSyncPage /></ProtectedRoute>} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/appointments/:id" element={<AppointmentDetailPage />} />
          <Route path="/staff/queue" element={<PageStub title="Staff Queue" />} />
          <Route path="/staff/patients/:id" element={<ProtectedRoute><StaffPatientProfilePage /></ProtectedRoute>} />
          <Route path="/staff/coding/:encounterId" element={<ProtectedRoute><CodeVerificationPage /></ProtectedRoute>} />
          <Route path="/admin/users" element={<AdminUserManagementPage />} />
          <Route path="/admin/audit-log" element={<AdminAuditLogPage />} />
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
