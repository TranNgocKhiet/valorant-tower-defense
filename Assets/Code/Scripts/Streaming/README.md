# Game Live Streaming Feature

This directory contains the implementation of the live streaming feature for the tower defense game.

## Directory Structure

```
Streaming/
├── Core/                      # Core enums and types
│   ├── ConnectionState.cs     # Streaming connection states
│   └── QualityLevel.cs        # Frame quality levels
├── Models/                    # Data models
│   ├── GameStateMetadata.cs   # Game state information
│   ├── GameplayFrame.cs       # Raw captured frame
│   ├── EncodedFrame.cs        # Encoded frame for transmission
│   ├── StreamingStats.cs      # Session statistics
│   ├── StreamingConfig.cs     # Configuration settings
│   ├── SessionInitMessage.cs  # Session initialization message
│   ├── SessionTerminateMessage.cs  # Session termination message
│   └── TransmissionResult.cs  # Transmission result
├── Interfaces/                # Service interfaces
│   ├── IStreamManager.cs      # Main orchestration interface
│   ├── IFrameCaptureService.cs  # Frame capture interface
│   ├── IFrameEncoder.cs       # Frame encoding interface
│   ├── IFrameBuffer.cs        # Frame buffering interface
│   ├── ITransmissionService.cs  # HTTP transmission interface
│   ├── IAuthManager.cs        # Authentication interface
│   └── IConfigManager.cs      # Configuration interface
└── Services/                  # Service implementations (to be added)
```

## Architecture Overview

The streaming system follows a producer-consumer pattern with three main layers:

1. **Capture Layer**: Captures gameplay frames from Unity's rendering pipeline
2. **Processing Layer**: Encodes, compresses, and buffers frames
3. **Transmission Layer**: Sends frames to the API endpoint with retry logic

## Key Components

### StreamManager
Orchestrates the entire streaming lifecycle, manages connection state transitions, and coordinates all services.

### FrameCaptureService
Captures frames from Unity's rendering pipeline using RenderTexture and extracts game state metadata.

### FrameEncoder
Compresses frame data and encodes it to JSON format with base64-encoded visual data.

### FrameBuffer
Maintains a circular buffer of encoded frames with size limits (30 seconds, 100 MB max).

### TransmissionService
Sends frames to API endpoint via HTTP POST with retry logic and exponential backoff.

### AuthManager
Retrieves and refreshes authentication tokens from AWS Cognito.

### ConfigManager
Persists streaming configuration using PlayerPrefs and validates settings.

## Connection States

- **Idle**: No streaming activity
- **Connecting**: Attempting to establish connection
- **Connected**: Connection established, session not yet initialized
- **Streaming**: Actively streaming gameplay frames
- **Disconnected**: Connection lost during streaming
- **Reconnecting**: Attempting to reconnect after connection loss
- **Terminating**: Terminating the streaming session
- **Error**: Error state requiring user intervention

## Quality Levels

- **Low**: 640x360 resolution
- **Medium**: 1280x720 resolution
- **High**: 1920x1080 resolution

## Requirements Mapping

This implementation satisfies requirements 1.5, 2.1, 2.3, 2.4, 4.3, 9.1, 9.2, and 9.4 from the requirements document.

## Next Steps

1. Implement ConfigManager (Task 2)
2. Implement AuthManager (Task 3)
3. Implement FrameCaptureService (Task 5)
4. Implement FrameEncoder (Task 6)
5. Implement FrameBuffer (Task 8)
6. Implement TransmissionService (Task 9)
7. Implement StreamManager (Task 11)
8. Implement UIController (Task 14)
9. Integration and wiring (Task 15)
