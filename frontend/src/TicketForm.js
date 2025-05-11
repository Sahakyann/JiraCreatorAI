import { useState } from 'react';
import axios from 'axios';

export default function TicketForm() {
  const [input, setInput] = useState('');
  const [ticket, setTicket] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await axios.post('https://localhost:7282/api/Ticket/generate', { input });
      setTicket(res.data.ticket);
    } catch (err) {
      console.error(err);
      setTicket('Failed to generate ticket.');
    } finally {
      setLoading(false);
    }
  }; 

  return (
    <form onSubmit={handleSubmit}>
      <textarea
        rows="5"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        placeholder="Describe the issue here..."
        style={{ width: '100%', padding: '1rem', fontSize: '1rem' }}
      />
      <button type="submit" disabled={loading} style={{ marginTop: '1rem' }}>
        {loading ? 'Generating...' : 'Generate Ticket'}
      </button>
      {ticket && (
        <pre style={{ marginTop: '2rem', whiteSpace: 'pre-wrap', background: '#f0f0f0', padding: '1rem' }}>
          {ticket}
        </pre>
      )}
    </form>
  );
}
