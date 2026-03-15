import json
import boto3
from decimal import Decimal

# Helper to handle DynamoDB numbers (Decimals) which json.dumps can't process
class DecimalEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, Decimal):
            return int(obj)
        return super(DecimalEncoder, self).default(obj)

dynamodb = boto3.resource('dynamodb', region_name='ap-southeast-1')
table = dynamodb.Table('td-PlayerData')

def lambda_handler(event, context):
    try:
        # 1. Get PlayerID from Query String Parameters (?PlayerID=xyz)
        query_params = event.get('queryStringParameters', {})
        p_id = query_params.get('PlayerID')

        if not p_id:
            return {
                'statusCode': 400,
                'headers': {"Access-Control-Allow-Origin": "*"},
                'body': json.dumps({'error': 'Missing PlayerID parameter'})
            }

        # 2. Fetch from DynamoDB with composite key
        # Use "player-data" as StreamDomain for regular player progress
        response = table.get_item(
            Key={
                'PlayerID': p_id,
                'StreamDomain': 'player-data'  # Fixed: Added sort key
            }
        )
        
        # 3. Handle if player is not found
        if 'Item' not in response:
            # Return a default starting set of data
            default_data = {
                'PlayerID': p_id,
                'Radianite': 0,
                'MaxLevel': 1
            }
            return {
                'statusCode': 200,
                'headers': {
                    "Content-Type": "application/json",
                    "Access-Control-Allow-Origin": "*"
                },
                'body': json.dumps(default_data)
            }

        # 4. Return the found data (exclude StreamDomain from response)
        item = response['Item']
        player_data = {
            'PlayerID': item.get('PlayerID'),
            'Radianite': item.get('Radianite', 0),
            'MaxLevel': item.get('MaxLevel', 1)
        }
        
        return {
            'statusCode': 200,
            'headers': {
                "Content-Type": "application/json",
                "Access-Control-Allow-Origin": "*"
            },
            'body': json.dumps(player_data, cls=DecimalEncoder)
        }

    except Exception as e:
        print(f"Error: {str(e)}")
        return {
            'statusCode': 500,
            'headers': {"Access-Control-Allow-Origin": "*"},
            'body': json.dumps({'error': str(e)})
        }