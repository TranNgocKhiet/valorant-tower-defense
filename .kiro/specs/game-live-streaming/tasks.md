# Implementation Plan: Game Live Streaming

## Overview

This implementation plan breaks down the game live streaming feature into discrete coding tasks. The feature will be implemented in C# for Unity, following the architecture defined in the design document. Tasks are organized to build incrementally, with early validation of core functionality and property-based tests integrated throughout.

## Tasks

- [ ] 1. Set up project structure and core interfaces
  - Create directory structure for streaming components
  - Define core enums (ConnectionState, QualityLevel)
  - Define data model classes (GameplayFrame, GameStateMetadata, EncodedFrame, StreamingStats, StreamingConfig)
  - Define interface contracts for all services
  - Set up NUnit and FsCheck testing framework
  - _Requirements: 1.5, 2.1, 2.3, 2.4, 4.3, 9.1, 9.2, 9.4_

- [ ]* 1.1 Write property test for configuration persistence
  - **Property 10: Configuration Persistence Round-Trip**
  - **Validates: Requirements 4.6**

- [ ] 2. Implement ConfigManager for streaming settings
  - [ ] 2.1 Create ConfigManager class with PlayerPrefs integration
    - Implement LoadConfig() to retrieve persisted settings
    - Implement SaveConfig() to persist settings
    - Implement ValidateConfig() with validation rules (frame rate 1-30, valid URL format)
    - Implement default configuration fallback
    - _Requirements: 4.1, 4.2, 4.3, 4.6, 1.4_
  
  - [ ]* 2.2 Write unit tests for ConfigManager
    - Test configuration validation edge cases (invalid URLs, out-of-range frame rates)
    - Test persistence and loading
    - Test default fallback behavior
    - _Requirements: 4.1, 4.2, 4.3, 4.6_

- [ ]* 2.3 Write property test for URL validation
  - **Property 3: URL Validation Before Connection**
  - **Validates: Requirements 1.4**

- [ ] 3. Implement AuthManager for AWS authentication
  - [ ] 3.1 Create AuthManager class with AWS SDK integration
    - Implement GetAuthTokenAsync() to retrieve tokens from AWS Cognito
    - Implement RefreshTokenAsync() to refresh expired tokens
    - Implement IsTokenValid() to check token expiration
    - _Requirements: 8.1, 8.2, 8.4_
  
  - [ ]* 3.2 Write unit tests for AuthManager
    - Test token retrieval
    - Test token refresh logic
    - Test token validation
    - _Requirements: 8.1, 8.2, 8.4_

- [ ]* 3.3 Write property test for authentication token inclusion
  - **Property 19: Authentication Token in Session Requests**
  - **Validates: Requirements 8.1, 8.2**

- [ ] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Implement FrameCaptureService for gameplay frame capture
  - [ ] 5.1 Create FrameCaptureService class with RenderTexture integration
    - Implement InitializeCapture() to set up RenderTexture based on quality level
    - Implement CaptureFrame() to capture visual data and game state metadata
    - Implement SetFrameRate() to configure capture rate
    - Implement SetQuality() to adjust resolution
    - Add UTC timestamp to each frame
    - Add sequence number generation
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.4, 4.5_
  
  - [ ]* 5.2 Write unit tests for FrameCaptureService
    - Test frame capture with different quality settings
    - Test frame rate configuration
    - Test metadata extraction
    - Test error handling for capture failures
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ]* 5.3 Write property test for frame data completeness
  - **Property 4: Frame Data Completeness**
  - **Validates: Requirements 2.2, 2.3, 2.4, 9.2, 9.4**

- [ ]* 5.4 Write property test for quality setting resolution
  - **Property 9: Quality Setting Affects Resolution**
  - **Validates: Requirements 4.4, 4.5**

- [ ] 6. Implement FrameEncoder for frame compression and encoding
  - [ ] 6.1 Create FrameEncoder class with async encoding
    - Implement EncodeAsync() to compress and encode frames on background thread
    - Implement JPEG compression using Unity ImageConversion
    - Implement base64 encoding for visual data
    - Implement JSON serialization with format version
    - Implement SetCompressionQuality() for adjustable compression
    - _Requirements: 3.2, 9.1, 9.2, 9.3, 9.4_
  
  - [ ]* 6.2 Write unit tests for FrameEncoder
    - Test encoding with different compression qualities
    - Test base64 encoding/decoding
    - Test JSON serialization
    - _Requirements: 3.2, 9.1, 9.3_

- [ ]* 6.3 Write property test for frame compression
  - **Property 7: Frame Compression Before Transmission**
  - **Validates: Requirements 3.2**

- [ ]* 6.4 Write property test for frame encoding round-trip
  - **Property 21: Frame Encoding Round-Trip**
  - **Validates: Requirements 9.1, 9.5**

- [ ]* 6.5 Write property test for base64 encoding
  - **Property 22: Base64 Encoding of Visual Data**
  - **Validates: Requirements 9.3**

- [ ] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Implement FrameBuffer for frame buffering
  - [ ] 8.1 Create FrameBuffer class with circular buffer
    - Implement thread-safe Enqueue() and Dequeue() operations
    - Implement TryDequeue() for non-blocking dequeue
    - Implement memory usage tracking
    - Implement automatic oldest-frame dropping when buffer is full
    - Implement size limits (30 seconds of frames, 100 MB max)
    - Implement Clear() for buffer cleanup
    - _Requirements: 6.5, 10.1, 10.3, 10.5_
  
  - [ ]* 8.2 Write unit tests for FrameBuffer
    - Test enqueue/dequeue operations
    - Test buffer size limits
    - Test memory usage tracking
    - Test thread safety
    - _Requirements: 6.5, 10.1, 10.3_

- [ ]* 8.3 Write property test for memory usage limit
  - **Property 23: Memory Usage Limit**
  - **Validates: Requirements 10.1**

- [ ]* 8.4 Write property test for buffered frame transmission order
  - **Property 18: Buffered Frame Transmission Order**
  - **Validates: Requirements 6.6**

- [ ] 9. Implement TransmissionService for HTTP communication
  - [ ] 9.1 Create TransmissionService class with UnityWebRequest
    - Implement SendFrameAsync() with retry logic and exponential backoff
    - Implement SendSessionInitAsync() for session initialization
    - Implement SendSessionTerminateAsync() for session termination
    - Implement SetEndpoint() for API URL configuration
    - Add authentication token headers to all requests
    - Add session ID headers to frame requests
    - _Requirements: 1.1, 1.2, 3.1, 3.3, 3.4, 5.1, 8.1_
  
  - [ ]* 9.2 Write unit tests for TransmissionService
    - Test successful transmission
    - Test retry logic with mock failures
    - Test exponential backoff timing
    - Test session init/terminate messages
    - _Requirements: 1.1, 1.2, 3.4, 5.1_

- [ ]* 9.3 Write property test for retry logic
  - **Property 8: Retry Logic with Exponential Backoff**
  - **Validates: Requirements 3.4**

- [ ]* 9.4 Write property test for session initialization message
  - **Property 2: Session Initialization Message Completeness**
  - **Validates: Requirements 1.2**

- [ ]* 9.5 Write property test for session termination message
  - **Property 11: Session Termination Message Sent**
  - **Validates: Requirements 5.1**

- [ ] 10. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Implement StreamManager orchestration and state management
  - [ ] 11.1 Create StreamManager MonoBehaviour with state machine
    - Implement state machine with all ConnectionState transitions
    - Implement StartStreaming() to initiate streaming
    - Implement StopStreaming() to terminate streaming
    - Implement GetConnectionState() and GetStats() accessors
    - Implement UpdateConfiguration() for runtime config changes
    - Define events (OnConnectionStateChanged, OnError, OnStatsUpdated)
    - _Requirements: 1.1, 1.5, 5.1, 5.2, 5.3, 6.1_
  
  - [ ] 11.2 Implement connection establishment logic
    - Implement connection coroutine with 5-second timeout
    - Implement URL validation before connection attempt
    - Implement session initialization message sending
    - Implement error handling for unreachable endpoint
    - _Requirements: 1.1, 1.3, 1.4, 1.5_
  
  - [ ] 11.3 Implement frame capture and transmission loop
    - Implement coroutine for periodic frame capture at configured frame rate
    - Integrate FrameCaptureService for frame capture
    - Integrate FrameEncoder for frame encoding
    - Integrate FrameBuffer for frame buffering
    - Integrate TransmissionService for frame transmission
    - Implement asynchronous transmission to avoid blocking game loop
    - _Requirements: 2.1, 3.1, 3.3, 6.6_
  
  - [ ] 11.4 Implement reconnection logic
    - Implement reconnection coroutine with 5-second intervals
    - Implement 60-second maximum reconnection duration
    - Implement frame buffering during reconnection
    - Implement buffered frame transmission after reconnection
    - Implement reconnection failure handling
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  
  - [ ] 11.5 Implement session termination logic
    - Implement session termination message sending
    - Implement connection closure with 2-second timeout
    - Implement resource cleanup (RenderTexture, buffers, coroutines)
    - Implement automatic termination on game exit
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [ ] 11.6 Implement token refresh coroutine
    - Implement periodic token validation (every 60 seconds)
    - Implement automatic token refresh before expiration
    - Implement error handling for token refresh failures
    - _Requirements: 8.4, 8.5_
  
  - [ ]* 11.7 Write unit tests for StreamManager
    - Test state machine transitions
    - Test connection establishment
    - Test session termination
    - Test reconnection logic
    - Test resource cleanup
    - Test error handling scenarios
    - _Requirements: 1.1, 1.3, 1.5, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4_

- [ ]* 11.8 Write property test for connection establishment
  - **Property 1: Connection Establishment**
  - **Validates: Requirements 1.1, 1.5**

- [ ]* 11.9 Write property test for frame transmission during streaming
  - **Property 6: Frame Transmission During Active Streaming**
  - **Validates: Requirements 3.1**

- [ ]* 11.10 Write property test for state transitions
  - **Property 12: State Transitions on Lifecycle Events**
  - **Validates: Requirements 1.5, 5.3, 6.1**

- [ ]* 11.11 Write property test for connection closure
  - **Property 13: Connection Closure on Termination**
  - **Validates: Requirements 5.2, 5.3**

- [ ]* 11.12 Write property test for resource release
  - **Property 14: Resource Release on Termination**
  - **Validates: Requirements 5.4**

- [ ]* 11.13 Write property test for reconnection attempts
  - **Property 15: Reconnection Attempts After Connection Loss**
  - **Validates: Requirements 6.2**

- [ ]* 11.14 Write property test for streaming resumption
  - **Property 16: Streaming Resumption After Reconnection**
  - **Validates: Requirements 6.3**

- [ ]* 11.15 Write property test for frame buffering during reconnection
  - **Property 17: Frame Buffering During Reconnection**
  - **Validates: Requirements 6.5**

- [ ]* 11.16 Write property test for automatic token refresh
  - **Property 20: Automatic Token Refresh**
  - **Validates: Requirements 8.4**

- [ ] 12. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Implement resource monitoring and adaptive adjustments
  - [ ] 13.1 Create resource monitoring coroutine in StreamManager
    - Implement memory usage monitoring
    - Implement CPU usage estimation
    - Implement adaptive buffer size reduction when memory exceeds 90 MB
    - Implement adaptive frame rate reduction when CPU exceeds 15%
    - Implement frame memory release after successful transmission
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
  
  - [ ]* 13.2 Write unit tests for resource monitoring
    - Test memory usage tracking
    - Test CPU usage estimation
    - Test adaptive buffer size reduction
    - Test adaptive frame rate reduction
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [ ]* 13.3 Write property test for adaptive buffer size reduction
  - **Property 24: Adaptive Buffer Size Reduction**
  - **Validates: Requirements 10.3**

- [ ]* 13.4 Write property test for adaptive frame rate reduction
  - **Property 25: Adaptive Frame Rate Reduction**
  - **Validates: Requirements 10.4**

- [ ]* 13.5 Write property test for frame memory release
  - **Property 26: Frame Memory Release After Transmission**
  - **Validates: Requirements 10.5**

- [ ]* 13.6 Write property test for configurable frame rate
  - **Property 5: Configurable Frame Rate**
  - **Validates: Requirements 2.1**

- [ ] 14. Implement UIController for streaming status display
  - [ ] 14.1 Create UIController MonoBehaviour with UI elements
    - Create streaming indicator UI element
    - Create connection state display
    - Create frame rate display
    - Create session duration display
    - Create error notification system
    - Create configuration interface (API URL, frame rate, quality settings)
    - _Requirements: 4.1, 4.2, 4.3, 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [ ] 14.2 Wire UIController to StreamManager events
    - Subscribe to OnConnectionStateChanged event
    - Subscribe to OnError event
    - Subscribe to OnStatsUpdated event
    - Update UI elements based on streaming state
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [ ]* 14.3 Write unit tests for UIController
    - Test UI updates on state changes
    - Test error notification display
    - Test configuration interface
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 15. Integration and wiring
  - [ ] 15.1 Create StreamingSystem prefab with all components
    - Add StreamManager to prefab
    - Add UIController to prefab
    - Configure component references
    - Set default configuration values
    - _Requirements: All_
  
  - [ ] 15.2 Integrate with existing game systems
    - Hook into game camera for frame capture
    - Hook into game state manager for metadata extraction
    - Hook into AWS authentication system
    - Hook into game exit event for automatic termination
    - _Requirements: 2.3, 5.5, 8.2_
  
  - [ ]* 15.3 Write integration tests
    - Test end-to-end streaming flow with mock API
    - Test reconnection with simulated connection loss
    - Test resource cleanup on game exit
    - Test performance impact (maintain 30+ FPS)
    - _Requirements: 3.6, 5.5, 6.1, 6.2, 6.3_

- [ ] 16. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties (26 properties total)
- Unit tests validate specific examples and edge cases
- All code is implemented in C# for Unity
- FsCheck is used for property-based testing with minimum 100 iterations per test
