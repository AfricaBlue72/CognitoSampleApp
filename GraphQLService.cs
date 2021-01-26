using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognitoSampleApp
{
    class GraphQLService
    {
        public void CreateClient()
        {
            var graphQLClient = new GraphQLHttpClient("https://api.example.com/graphql", new NewtonsoftJsonSerializer());
            //graphQLClient.Options.WebSocketEndPoint;
        }
    }


}
