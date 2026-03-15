import json
import boto3
from datetime import datetime
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('td-PlayerData')  # Updated table name

def decimal_default(obj):
    """Helper to serialize Decimal objects to JSON"""
    if isinstance(obj, Decimal):
        return int(obj) if obj % 1 == 0 else float(obj)
    raise TypeError

def lambda_handler(event, context):
    """
    Handles stream info operations:
    - POST: Save/update stream info when streaming starts
    - DELETE: Remove stream info when streaming ends
    - GET: Retrieve active streams list
    """
    
    http_method = event.get('httpMethod', '')
    
    try:
        if http_method == 'POST':
            return handle_save_stream(event)
        elif http_method == 'DELETE':
            return handle_delete_stream(event)
        elif http_method == 'GET':
            return handle_get_active_streams(event)
        else:
            return {
                'statusCode': 405,
                'headers': {
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
                    'Access-Control-Allow-Headers': 'Content-Type'
                },
                'body': json.dumps({'error': 'Method not allowed'})
            }
    except Exception as e:
        print(f"Error: {str(e)}")
        return {
            'statusCode': 500,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type'
            },
            'body': json.dumps({'error': str(e)})
        }

def handle_save_stream(event):
    """Save or update stream info in DynamoDB"""
    body = json.loads(event['body'])
    
    stream_domain = body.get('StreamDomain')
    player_id = body.get('PlayerID')
    stream_start_time = body.get('StreamStartTime')
    current_level = body.get('CurrentLevel', 1)
    status = body.get('Status', 'active')
    session_id = body.get('SessionId', '')
    
    if not stream_domain or not player_id:
        return {
            'statusCode': 400,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type'
            },
            'body': json.dumps({'error': 'StreamDomain and PlayerID are required'})
        }
    
    # Store stream info in the same table with a composite key
    # Using StreamDomain as sort key to allow multiple streams per player
    table.put_item(
        Item={
            'PlayerID': player_id,
            'StreamDomain': stream_domain,
            'StreamStartTime': stream_start_time,
            'CurrentLevel': current_level,
            'Status': status,
            'SessionId': session_id,
            'LastUpdated': datetime.utcnow().isoformat()
        }
    )
    
    return {
        'statusCode': 200,
        'headers': {
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        },
        'body': json.dumps({
            'message': 'Stream info saved successfully',
            'StreamDomain': stream_domain
        })
    }

def handle_delete_stream(event):
    """Delete stream info from DynamoDB"""
    query_params = event.get('queryStringParameters', {})
    
    stream_domain = query_params.get('StreamDomain')
    player_id = query_params.get('PlayerID')
    
    if not stream_domain or not player_id:
        return {
            'statusCode': 400,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type'
            },
            'body': json.dumps({'error': 'StreamDomain and PlayerID are required'})
        }
    
    # Delete the stream info item
    table.delete_item(
        Key={
            'PlayerID': player_id,
            'StreamDomain': stream_domain
        }
    )
    
    return {
        'statusCode': 200,
        'headers': {
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        },
        'body': json.dumps({
            'message': 'Stream info deleted successfully'
        })
    }

def handle_get_active_streams(event):
    """Retrieve all active streams from DynamoDB"""
    
    # Scan for all items with Status = 'active'
    # Note: In production, consider using a GSI for better performance
    response = table.scan(
        FilterExpression='attribute_exists(StreamDomain) AND #status = :active',
        ExpressionAttributeNames={
            '#status': 'Status'
        },
        ExpressionAttributeValues={
            ':active': 'active'
        }
    )
    
    streams = response.get('Items', [])
    
    # Filter out items that don't have stream info (regular player data)
    active_streams = [
        {
            'StreamDomain': item.get('StreamDomain'),
            'PlayerID': item.get('PlayerID'),
            'StreamStartTime': item.get('StreamStartTime'),
            'CurrentLevel': item.get('CurrentLevel', 1),
            'Status': item.get('Status'),
            'SessionId': item.get('SessionId', '')
        }
        for item in streams
        if 'StreamDomain' in item
    ]
    
    return {
        'statusCode': 200,
        'headers': {
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        },
        'body': json.dumps({
            'streams': active_streams
        }, default=decimal_default)
    }
