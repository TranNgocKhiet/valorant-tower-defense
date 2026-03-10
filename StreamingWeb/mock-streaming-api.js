const express = require('express');
const path = require('path');
const app = express();

// Store latest frame for viewer
let latestFrame = null;
let sessionData = null;

// Enable CORS for all routes
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
  
  // Handle preflight requests
  if (req.method === 'OPTIONS') {
    return res.sendStatus(200);
  }
  
  next();
});

app.use(express.json({ limit: '50mb' }));

// Request logging middleware
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.path}`);
  next();
});

// Serve the viewer HTML page
app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, 'viewer.html'));
});

// Get latest frame for viewer
app.get('/stream/latest', (req, res) => {
  if (latestFrame) {
    res.json({ 
      success: true, 
      frame: {
        ...latestFrame,
        sessionId: sessionData?.sessionId
      }
    });
  } else {
    res.json({ success: true, frame: null });
  }
});

// Session initialization
app.post('/stream/init', (req, res) => {
  sessionData = {
    sessionId: `session-${Date.now()}`,
    playerId: req.body.playerId,
    gameVersion: req.body.gameVersion,
    config: req.body.config,
    startTime: new Date()
  };
  
  console.log('📡 Session Init:', sessionData);
  
  res.json({ 
    sessionId: sessionData.sessionId, 
    success: true 
  });
});

// Frame reception
app.post('/stream/frame', (req, res) => {
  // Store latest frame for viewer
  const body = req.body;
  
  latestFrame = {
    sequenceNumber: body.sequenceNumber || body.SequenceNumber,
    timestamp: body.timestamp || body.Timestamp,
    visualData: body.visualData || body.VisualData || body.visualDataBase64 || body.VisualDataBase64,
    metadata: body.metadata || body.Metadata || {}
  };
  
  const sequenceNumber = latestFrame.sequenceNumber || 'unknown';
  const metadata = latestFrame.metadata;
  
  // Log frame info (without the large base64 data)
  console.log(`📦 Frame ${sequenceNumber} received:`, {
    wave: metadata.currentWave || metadata.CurrentWave || 0,
    enemies: metadata.enemyCount || metadata.EnemyCount || 0,
    towers: metadata.towerCount || metadata.TowerCount || 0,
    health: metadata.playerHealth || metadata.PlayerHealth || 0,
    score: metadata.score || metadata.Score || 0,
    hasVisualData: !!latestFrame.visualData
  });
  
  res.json({ 
    success: true, 
    sequenceNumber: sequenceNumber 
  });
});

// Debug endpoint to see raw frame data
app.post('/stream/frame/debug', (req, res) => {
  console.log('🔍 DEBUG - Full frame data:', JSON.stringify(req.body, null, 2));
  res.json({ success: true, debug: true });
});

// Session termination
app.post('/stream/terminate', (req, res) => {
  console.log('🛑 Session Terminated:', {
    sessionId: req.body.sessionId,
    stats: req.body.stats
  });
  
  // Clear stored data
  latestFrame = null;
  sessionData = null;
  
  res.json({ success: true });
});

// 404 handler
app.use((req, res) => {
  console.log(`❌ 404 Not Found: ${req.method} ${req.path}`);
  res.status(404).json({ 
    success: false, 
    error: 'Endpoint not found',
    path: req.path 
  });
});

// Error handler
app.use((err, req, res, next) => {
  console.error('❌ Server Error:', err);
  res.status(500).json({ 
    success: false, 
    error: err.message 
  });
});

const PORT = 3000;
app.listen(PORT, () => {
  console.log(`🚀 Mock Streaming API running on http://localhost:${PORT}`);
  console.log('📋 Available endpoints:');
  console.log('   POST /stream/init      - Initialize streaming session');
  console.log('   POST /stream/frame     - Receive frame data');
  console.log('   POST /stream/terminate - Terminate session');
  console.log('✅ CORS enabled for all origins');
  console.log('Ready to receive streaming data!');
});
