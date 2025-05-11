import { useState } from 'react';
import axios from 'axios';

export default function TicketForm() {
  const [input, setInput] = useState('');
  const [seleniumStepsJson, setSeleniumStepsJson] = useState(null);
  const [parsedSteps, setParsedSteps] = useState([]);
  const [ticket, setTicket] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const payload = { input, seleniumStepsJson };
      const res = await axios.post('https://localhost:7282/api/Ticket/generate', payload);
      setTicket(res.data.ticket);
    } catch (err) {
      console.error(err);
      setTicket('Failed to generate ticket.');
    } finally {
      setLoading(false);
    }
  };

  const handleFileUpload = (e) => {
    const file = e.target.files[0];
    const reader = new FileReader();
    reader.onload = () => {
      const fileContent = reader.result;
      setSeleniumStepsJson(fileContent);
      try {
        const json = JSON.parse(fileContent);
        const commands = json.commands || [];
        const steps = [];
        let stepNumber = 1;

        for (const cmd of commands) {
          const action = cmd.command?.toLowerCase() || '';
          const target = cmd.target || '';
          const value = cmd.value || '';
          let humanStep = '';

          switch (action) {
            case 'open':
              humanStep = `Navigate to ${target}`;
              break;
            case 'click':
              humanStep = `Click on element '${target}'`;
              break;
            case 'type':
              humanStep = `Type '${value}' into element '${target}'`;
              break;
            case 'select':
              humanStep = `Select '${value}' from dropdown '${target}'`;
              break;
            default:
              humanStep = `Perform '${action}' on '${target}' ${value}`.trim();
              break;
          }

          steps.push(`${stepNumber++}. ${humanStep}`);
        }

        setParsedSteps(steps);
      } catch (err) {
        console.error('Failed to parse Katalon JSON', err);
        setParsedSteps(['Error parsing file.']);
      }
    };
    reader.readAsText(file);
  };

  return (
    <form onSubmit={handleSubmit} style={styles.form}>
      <h2 style={styles.heading}>Ticket Generator</h2>

      <textarea
        rows="5"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        placeholder="Describe the issue here..."
        style={styles.textarea}
      />

      <div style={styles.fileUploadContainer}>
        <label style={styles.fileLabel}>
          Upload Selenium IDE JSON:
          <input type="file" accept=".json" onChange={handleFileUpload} style={styles.fileInput} />
        </label>
        <small style={styles.smallText}>Optional: Upload to auto-generate Steps to Reproduce</small>
      </div>

      {parsedSteps.length > 0 && (
        <div style={styles.stepsContainer}>
          <h3 style={styles.stepsHeading}>Parsed Steps Preview:</h3>
          <ul style={styles.stepsList}>
            {parsedSteps.map((step, idx) => (
              <li key={idx} style={styles.stepItem}>{step}</li>
            ))}
          </ul>
        </div>
      )}

      <button type="submit" disabled={loading} style={loading ? styles.buttonLoading : styles.button}>
        {loading ? 'Generating...' : 'Generate Ticket'}
      </button>

      {ticket && (
        <pre style={styles.ticketPreview}>
          {ticket}
        </pre>
      )}
    </form>
  );
}

const styles = {
  form: {
    maxWidth: '600px',
    margin: '2rem auto',
    padding: '2rem',
    backgroundColor: '#ffffff',
    borderRadius: '10px',
    boxShadow: '0 4px 8px rgba(0,0,0,0.1)',
    fontFamily: 'Arial, sans-serif',
  },
  heading: {
    textAlign: 'center',
    marginBottom: '1.5rem',
    fontSize: '1.8rem',
    color: '#333',
  },
  textarea: {
    width: '100%',
    height: '300px',
    fontSize: '1rem',
    borderRadius: '6px',
    border: '1px solid #ccc',
    resize: 'vertical',
    marginBottom: '1rem',
  },
  fileUploadContainer: {
    marginBottom: '1.5rem',
  },
  fileLabel: {
    display: 'block',
    marginBottom: '0.5rem',
    fontWeight: 'bold',
  },
  fileInput: {
    display: 'block',
    marginTop: '0.5rem',
  },
  smallText: {
    display: 'block',
    marginTop: '0.5rem',
    fontSize: '0.85rem',
    color: '#666',
  },
  stepsContainer: {
    marginTop: '2rem',
    padding: '1rem',
    backgroundColor: '#f8f9ff',
    borderRadius: '8px',
    border: '1px solid #d0d7ff',
  },
  stepsHeading: {
    marginBottom: '1rem',
    fontSize: '1.2rem',
    color: '#333',
  },
  stepsList: {
    paddingLeft: '1.2rem',
    lineHeight: '1.6',
  },
  stepItem: {
    marginBottom: '0.5rem',
  },
  button: {
    width: '100%',
    padding: '0.75rem',
    fontSize: '1rem',
    backgroundColor: '#4CAF50',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    cursor: 'pointer',
    transition: 'background-color 0.3s',
  },
  buttonLoading: {
    width: '100%',
    padding: '0.75rem',
    fontSize: '1rem',
    backgroundColor: '#999',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    cursor: 'not-allowed',
  },
  ticketPreview: {
    marginTop: '2rem',
    whiteSpace: 'pre-wrap',
    backgroundColor: '#f0f0f0',
    padding: '1rem',
    borderRadius: '8px',
    fontSize: '1rem',
  }
};
