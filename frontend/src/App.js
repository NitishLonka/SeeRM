import { BrowserRouter, Routes, Route } from 'react-router-dom';
import CrmHomepage from './features/home/dashboard';
import NewCustomerForm from './features/customer-entry/customer-entry-form';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<CrmHomepage />} />
        <Route path="/customer-entry" element={<NewCustomerForm />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
