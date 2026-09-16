
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import './App.css'
import DashBoard from './pages/DashBoard';
import TasksBoard from './pages/TasksBoard';
import CommentsBoard from './pages/CommentsBoard';
import Toast from './components/Common/Toast'
import { useCacheNotification } from './hooks/useCacheNotification';

function App() {
    useCacheNotification();
    return (
        <BrowserRouter>
            <Toast />
            <Routes>
                <Route path="/" element={<DashBoard />} />
                <Route path="/projects/:projectId" element={<TasksBoard />} />
                <Route path="/projects/:projectId/tasks/:taskId" element={<CommentsBoard />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App
