using Amazon;

namespace Narochno.Credstash
{
    public class CredstashBuilder
    {
        public static Credstash WithRegion(RegionEndpoint region)
        {
            return new Credstash(new CredstashOptions() { Region = region }, new Amazon.KeyManagementService.AmazonKeyManagementServiceClient(region), new Amazon.DynamoDBv2.AmazonDynamoDBClient(region));
        }
    }
}
