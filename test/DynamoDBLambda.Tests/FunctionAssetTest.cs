using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Newtonsoft.Json;

using Xunit;

namespace DynamoDBLambda.Tests
{
    public class FunctionAssetTest : IDisposable
    { 
        string TableName { get; }
        IAmazonDynamoDB DDBClient { get; }
        
        public FunctionAssetTest()
        {
            this.TableName = "AssetPosition";
            this.DDBClient = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
        }

        [Fact]
        public async Task AssetPositionTestAsync()
        {
            TestLambdaContext context;
            APIGatewayProxyRequest request;
            APIGatewayProxyResponse response;

            Functions functions = new Functions(this.DDBClient, this.TableName);


            // Add a new asset position 
            AssetPosition myAssetPosition = new AssetPosition
            {
                Asset = "Itau CDB ate 2020",
                Amount = 1000,
                //DateTime = new DateTime(2018, 11, 26),
                Custodian = "XP"
            };

            TimePosition myTimePosition = new TimePosition() 
            { 
                Date = new DateTime(2018, 11, 26), 
                AssetPositions = new List<AssetPosition>() { myAssetPosition }
                };

            

            request = new APIGatewayProxyRequest
            {
                Body = JsonConvert.SerializeObject(myTimePosition)
            };
            context = new TestLambdaContext();
            response = await functions.AddTimePositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            var timePositionId = response.Body;

            // Confirm we can get the AssetPosition post back out
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, timePositionId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetTimePositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            TimePosition timePosition = JsonConvert.DeserializeObject<TimePosition>(response.Body);
            Assert.Equal(myAssetPosition.Asset, timePosition.AssetPositions.FirstOrDefault().Asset);
            Assert.Equal(myAssetPosition.Amount, timePosition.AssetPositions.FirstOrDefault().Amount);
            Assert.Equal(myTimePosition.Date, myTimePosition.Date);

            // List the AssetPositions
            request = new APIGatewayProxyRequest
            {
               //PathParameters = new Dictionary<string, string> { { "Id", myAssetPosition.Id.ToString() } }
            };
            context = new TestLambdaContext();
            response = await functions.GetTimePositionsAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            TimePosition[] timePositions = JsonConvert.DeserializeObject<TimePosition[]>(response.Body);
            //Assert.Single(assetPositions);

            var timePos = timePositions.Where(t => t.Id == int.Parse(timePositionId)).FirstOrDefault();

            Assert.NotNull(timePos);
            Assert.Equal(myAssetPosition.Asset, timePos.AssetPositions.FirstOrDefault().Asset);
            Assert.Equal(myAssetPosition.Amount, timePos.AssetPositions.FirstOrDefault().Amount);
            Assert.Equal(myTimePosition.Date, timePos.Date);


            // Delete the AssetPosition post
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, timePositionId } }
            };
            context = new TestLambdaContext();
            response = await functions.RemoveTimePositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            // Make sure the post was deleted.
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, timePositionId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetTimePositionAsync(request, context);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }

        public void Dispose()
        {
            this.DDBClient.Dispose();
        }
    }
}
