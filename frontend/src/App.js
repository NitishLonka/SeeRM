import { BrowserRouter, Routes, Route } from 'react-router-dom';
import CrmHomepage from './features/home/dashboard';
import NewCustomerForm from './features/customer-entry/customer-entry-form';
import LeadsPage from './features/leads/leads-page';
import LeadForm from './features/leads/lead-form';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<CrmHomepage />} />
        <Route path="/customer-entry" element={<NewCustomerForm />} />
        <Route path="/leads" element={<LeadsPage />} />
        <Route path="/leads/new" element={<LeadForm />} />
        <Route path="/leads/:id/edit" element={<LeadForm />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
