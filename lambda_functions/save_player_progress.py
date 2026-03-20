import json
import boto3
from decimal import Decimal

# Initialize DynamoDB
dynamodb = boto3.resource('dynamodb', region_name='ap-southeast-1')
table = dynamodb.Table('td-PlayerData')  # Fixed: Added closing quote

def lambda_handler(event, context):
    try:
        # 1. Parse the body. Handle cases where body might already be a dict
        if isinstance(event.get('body'), str):
            body = json.loads(event['body'], parse_float=Decimal)
        else:
            body = event.get('body', {})

        # 2. Extract values - must match Unity PlayerData class exactly!
        p_id = body.get('PlayerID')
        s_domain = body.get('StreamDomain', 'player-data')
        radianite = body.get('Radianite', 0)
        level = body.get('MaxLevel', 1)

        if not p_id:
            return {
                'statusCode': 400,
                'headers': {"Access-Control-Allow-Origin": "*"},
                'body': json.dumps({'error': 'Missing PlayerID'})
            }

        # 3. Save to DynamoDB with composite key
        # Use "player-data" as StreamDomain for regular player progress
        table.put_item(
            Item={
                'PlayerID': str(p_id),
                'StreamDomain': str(s_domain), # Linh hoạt theo từng loại dữ liệu
                'Radianite': int(radianite),
                'MaxLevel': int(level),
                'LastUpdated': str(context.aws_request_id) # Thêm ID request để dễ debug
            }
        )

        return {
            'statusCode': 200,
            'headers': {
                "Content-Type": "application/json",
                "Access-Control-Allow-Origin": "*"
            },
            'body': json.dumps({'message': f'Progress for {p_id} saved!'})
        }

    except Exception as e:
        print(f"Error: {str(e)}")  # This shows up in CloudWatch Logs
        return {
            'statusCode': 500,
            'headers': {"Access-Control-Allow-Origin": "*"},
            'body': json.dumps({'error': str(e)})
        }