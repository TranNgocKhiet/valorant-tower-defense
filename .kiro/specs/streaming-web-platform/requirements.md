# Requirements Document

## Introduction

This document defines the requirements for a streaming web platform that displays live gameplay streams from Unity game players. The platform consists of a frontend web application and a backend API service, both hosted on AWS. The system integrates with AWS DynamoDB to store and retrieve streaming session information, allowing users to browse active streamers and watch their gameplay in real-time.

## Glossary

- **Frontend**: The web-based user interface that displays the list of streamers and video player
- **Backend**: The Node.js/Express API service that handles streaming data and database operations
- **DynamoDB**: AWS NoSQL database service used to store streaming session information
- **Streaming_Session**: A record in DynamoDB representing an active or historical streaming session
- **Unity_Game**: The tower defense game that generates streaming frames via StreamManager
- **Streamer**: A player who is actively broadcasting their gameplay
- **Viewer**: A user who is watching a streamer's gameplay
- **Frame**: A single captured image from the Unity game with associated metadata
- **Session_ID**: Unique identifier for each streaming session
- **Player_ID**: Unique identifier for each game player/streamer

## Requirements

### Requirement 1: Display Active Streamers List

**User Story:** As a viewer, I want to see a list of currently active streamers, so that I can choose who to watch

#### Acceptance Criteria

1. WHEN the Frontend loads, THE Frontend SHALL fetch the list of active streaming sessions from the Backend
2. THE Frontend SHALL display each active streamer with their Player_ID, current game state, and session start time
3. WHEN a Streaming_Session becomes inactive, THE Frontend SHALL remove it from the displayed list within 10 seconds
4. THE Frontend SHALL refresh the streamer list every 5 seconds to show newly started sessions
5. WHEN no active streamers exist, THE Frontend SHALL display a message indicating no streams are available

### Requirement 2: Store Streaming Session Data

**User Story:** As a system administrator, I want streaming session information stored in DynamoDB, so that I can track active streams and historical data

#### Acceptance Criteria

1. WHEN the Backend receives a session initialization request, THE Backend SHALL create a new Streaming_Session record in DynamoDB
2. THE Streaming_Session record SHALL include Session_ID, Player_ID, start timestamp, game version, and streaming configuration
3. WHEN the Backend receives frame data, THE Backend SHALL update the Streaming_Session record with the latest frame timestamp and metadata
4. WHEN a session terminates, THE Backend SHALL update the Streaming_Session record with end timestamp and final statistics
5. THE Backend SHALL set a time-to-live (TTL) attribute on Streaming_Session records to automatically expire after 7 days

### Requirement 3: Stream Video Playback

**User Story:** As a viewer, I want to watch a selected streamer's gameplay, so that I can see their game in real-time

#### Acceptance Criteria

1. WHEN a Viewer selects a streamer from the list, THE Frontend SHALL display the video player for that Session_ID
2. THE Frontend SHALL poll the Backend for the latest frame every 33 milliseconds (targeting 30 FPS)
3. WHEN the Backend receives a frame request, THE Backend SHALL return the most recent frame for the requested Session_ID
4. THE Frontend SHALL decode the base64-encoded JPEG frame data and display it in the video player
5. WHEN no frames have been received for 10 seconds, THE Frontend SHALL display a "connection lost" message

### Requirement 4: Display Game State Metadata

**User Story:** As a viewer, I want to see the streamer's current game state, so that I can understand what is happening in their game

#### Acceptance Criteria

1. WHEN the Frontend receives a frame, THE Frontend SHALL extract and display the current wave number
2. THE Frontend SHALL display the count of active enemies in the game
3. THE Frontend SHALL display the count of towers placed by the player
4. THE Frontend SHALL display the player's current health value
5. THE Frontend SHALL display the player's current score

### Requirement 5: Backend API Endpoints

**User Story:** As a Unity_Game client, I want to send streaming data to the Backend, so that viewers can watch my gameplay

#### Acceptance Criteria

1. THE Backend SHALL provide a POST endpoint at /stream/init for session initialization
2. THE Backend SHALL provide a POST endpoint at /stream/frame for receiving frame data
3. THE Backend SHALL provide a POST endpoint at /stream/terminate for session termination
4. THE Backend SHALL provide a GET endpoint at /stream/active for retrieving active streaming sessions
5. THE Backend SHALL provide a GET endpoint at /stream/frame/:sessionId for retrieving the latest frame for a specific session
6. WHEN the Backend receives a request, THE Backend SHALL validate the request payload and return appropriate HTTP status codes

### Requirement 6: AWS Hosting and Deployment

**User Story:** As a system administrator, I want the web platform hosted on AWS, so that it is scalable and reliable

#### Acceptance Criteria

1. THE Frontend SHALL be hosted on AWS S3 with CloudFront distribution for content delivery
2. THE Backend SHALL be deployed on AWS Elastic Beanstalk or AWS Lambda with API Gateway
3. THE Backend SHALL connect to DynamoDB using AWS SDK with appropriate IAM credentials
4. THE Backend SHALL use environment variables for AWS region and DynamoDB table name configuration
5. THE Backend SHALL implement CORS headers to allow Frontend access from the CloudFront domain

### Requirement 7: Authentication and Authorization

**User Story:** As a system administrator, I want streaming sessions authenticated, so that only authorized players can stream

#### Acceptance Criteria

1. WHEN the Backend receives a session initialization request, THE Backend SHALL validate the authentication token
2. THE Backend SHALL use AWS Cognito tokens for authentication validation
3. IF the authentication token is invalid or expired, THEN THE Backend SHALL return HTTP 401 Unauthorized
4. THE Backend SHALL extract Player_ID from the validated authentication token
5. THE Backend SHALL allow unauthenticated read access to the active streams list and frame data for viewers

### Requirement 8: Error Handling and Resilience

**User Story:** As a viewer, I want the platform to handle errors gracefully, so that I have a good user experience even when issues occur

#### Acceptance Criteria

1. WHEN the Backend cannot connect to DynamoDB, THE Backend SHALL return HTTP 503 Service Unavailable
2. WHEN the Frontend cannot reach the Backend, THE Frontend SHALL display a connection error message and retry after 5 seconds
3. IF a frame request fails, THEN THE Frontend SHALL continue polling without disrupting the user interface
4. WHEN the Backend receives malformed frame data, THE Backend SHALL log the error and return HTTP 400 Bad Request
5. THE Backend SHALL implement request timeout of 30 seconds for all DynamoDB operations

### Requirement 9: Performance and Scalability

**User Story:** As a system administrator, I want the platform to handle multiple concurrent streams efficiently, so that it can scale to many users

#### Acceptance Criteria

1. THE Backend SHALL support at least 100 concurrent streaming sessions
2. THE Backend SHALL support at least 1000 concurrent viewers
3. THE Backend SHALL process frame uploads within 100 milliseconds (excluding network latency)
4. THE DynamoDB table SHALL use on-demand billing mode to automatically scale with traffic
5. THE Frontend SHALL implement efficient polling to minimize unnecessary network requests

### Requirement 10: Monitoring and Logging

**User Story:** As a system administrator, I want comprehensive logging and monitoring, so that I can troubleshoot issues and track usage

#### Acceptance Criteria

1. THE Backend SHALL log all session initialization, termination, and error events to CloudWatch Logs
2. THE Backend SHALL emit custom CloudWatch metrics for active session count and frame processing rate
3. WHEN an error occurs in the Backend, THE Backend SHALL log the error with stack trace and request context
4. THE Backend SHALL log the Session_ID and Player_ID for all streaming-related operations
5. THE Frontend SHALL log JavaScript errors to the browser console for debugging purposes

