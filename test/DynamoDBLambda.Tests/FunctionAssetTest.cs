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
                DateTime = new DateTime(2018, 11, 26),
                Custodian = "XP"
            };

            request = new APIGatewayProxyRequest
            {
                Body = JsonConvert.SerializeObject(myAssetPosition)
            };
            context = new TestLambdaContext();
            response = await functions.AddAssetPositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            var assetId = response.Body;

            // Confirm we can get the AssetPosition post back out
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, assetId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetAssetPositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            AssetPosition assetPosition = JsonConvert.DeserializeObject<AssetPosition>(response.Body);
            Assert.Equal(myAssetPosition.Asset, assetPosition.Asset);
            Assert.Equal(myAssetPosition.Amount, assetPosition.Amount);
            Assert.Equal(myAssetPosition.DateTime, assetPosition.DateTime);

            // List the AssetPositions
            request = new APIGatewayProxyRequest
            {
               //PathParameters = new Dictionary<string, string> { { "Id", myAssetPosition.Id.ToString() } }
            };
            context = new TestLambdaContext();
            response = await functions.GetAssetPositionsAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            AssetPosition[] assetPositions = JsonConvert.DeserializeObject<AssetPosition[]>(response.Body);
            //Assert.Single(assetPositions);

            var assetPos = assetPositions.Where(a => a.Id == int.Parse(assetId)).FirstOrDefault();

            Assert.NotNull(assetPos);
            Assert.Equal(myAssetPosition.Asset, assetPos.Asset);
            Assert.Equal(myAssetPosition.Amount, assetPos.Amount);
            Assert.Equal(myAssetPosition.DateTime, assetPos.DateTime);


            // Delete the AssetPosition post
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, assetId } }
            };
            context = new TestLambdaContext();
            response = await functions.RemoveAssetPositionAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            // Make sure the post was deleted.
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, assetId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetAssetPositionAsync(request, context);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }

        public void Dispose()
        {
            this.DDBClient.Dispose();
        }
    }
}
