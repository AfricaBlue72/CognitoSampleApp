namespace CognitoSampleApp
{
    static class ConfigData
    {
        #region Provided by the AWS Admin

        //Credentials to login into the Cognito User Pool
        public const string UserName = "";
        public const string Password = "!";

        //Constants for logging into the Cognito User Pool.
        //After successful login you obtain three tokens (Access, OpenId, Refresh).
        //There is one Cognito User Pool
        public const string UserPoolId = "";
        public const string ClientId = "";

        //Constants for logging into a Cognito Identity Pool
        //A Cognito Identity Pool gives you direct access to AWS Services using the OpenId token.
        //Authorization is determined by the roles attached to the User Group the user resides in.
        //Each AWS Account has its own Identity Pool which uses the one Cognito User Pool as Identity Provider.
        public const string IdentityPoolId = "";
        public const string IdentityProviderName = "";
        #endregion

        #region Your resources
        public const string QueueUrl = "";
        #endregion
    }
}
