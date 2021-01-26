﻿using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/* 
 * Amazon.Extensions.CognitoAuthentication
 * https://www.nuget.org/packages/Amazon.Extensions.CognitoAuthentication/2.0.3?_src=template
 * https://github.com/aws/aws-sdk-net-extensions-cognito/
 */

/* 
 * Microsoft.IdentityModel.JsonWebTokens
 * https://www.nuget.org/packages/Microsoft.IdentityModel.JsonWebTokens/6.8.0?_src=template
 */


/*
 * AWSSDK.S3
 */

//TODO: List S3 buckets - with test

namespace CognitoSampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int messageCount = 0;
       
        private const string _password = "HollandCasino1!";
        //Constants for obtaining tokens for API access.
        private const string UserPoolId = "eu-west-1_3BWJOmtrV";        //Provided by the AWS Admin
        private const string ClientId = "14uke6l2gv8cpkphg0jucmr3na";   //Provided by the AWS Admin

        //Constants for direct AWS Services access
        private const string IdentityPoolId = "eu-west-1:6a06788d-906c-45ae-9e54-e898d5337d95"; //Provided by the AWS Admin
        private const string IdentityProviderName = "cognito-idp.eu-west-1.amazonaws.com/eu-west-1_3BWJOmtrV"; //Provided by the AWS Admin

/*
        private const string _password = "";
        //Constants for obtaining tokens for API access.
        private const string UserPoolId = "eu-west-1_ttGnwSBBf";        //Provided by the AWS Admin
        //private const string ClientId = "1ail3lcjbj9kemc8dks7smbtv6";   //Provided by the AWS Admin
        private const string ClientId = "6qno3sijkd1t9flcb32ejnu38k";
        private const string ClientSecret = "1i42a13gbb8i5c2ts8rsa78f4k7u4c6bgrtv2r396m7is3cmfajs";

        //Constants for direct AWS Services access
        private const string IdentityPoolId = "eu-west-1:ed4d636d-5ac5-4b45-bb03-e34bb46c8180"; //Provided by the AWS Admin
        private const string IdentityProviderName = "cognito-idp.eu-west-1.amazonaws.com/eu-west-1_ttGnwSBBf"; //Provided by the AWS Admin
 */
        private const string QueueUrl = "https://sqs.eu-west-1.amazonaws.com/270577686952/TestQueue";

        private CognitoUser _cognitoUser = null;

        public CognitoUser User
        {
            get { return _cognitoUser; }
            set { _cognitoUser = value;  }
        }

        public async Task<string> GetIdToken()
        {
            if(User != null && User.SessionTokens != null && User.SessionTokens.IsValid() == false)
            {
                await GetCredsFromRefreshAsync(User.Username, User.SessionTokens.RefreshToken);
            }
            return User.SessionTokens.IdToken;
        }

        public async Task<string> GetAccessToken()
        {
            if (User != null && User.SessionTokens != null && User.SessionTokens.IsValid() == false)
            {
                await GetCredsFromRefreshAsync(User.Username, User.SessionTokens.RefreshToken);
            }
            return User.SessionTokens.AccessToken;
        }

        //Get AWS Credentials from the Id Token
        public async Task<CognitoAWSCredentials> GetAuthenticatedCredentials()
        {
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(
                IdentityPoolId, // Identity pool ID
                RegionEndpoint.EUWest1 // Region
            );

            string idName = IdentityProviderName;
            string idToken = await GetIdToken();
            credentials.AddLogin(idName, idToken);

            return credentials;
        }

        public MainWindow()
        {
            InitializeComponent();

            this.SendMessageButton.IsEnabled = false;
            this.ListBucketsButton.IsEnabled = false;
            this.RefreshButton.IsEnabled = false;
            this.GetMessageButton.IsEnabled = false;

            this.UserPool.Text = UserPoolId;
            this.IdentityPool.Text = IdentityProviderName;
            this.QueuUrlTextBox.Text = QueueUrl;

            this.Password.Password = _password;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string userName = this.UserName.Text;
            string password = this.Password.Password.ToString();

            if(userName != null && userName != string.Empty && password != null && password != string.Empty)
            {
                AuthFlowResponse authResponse = await AuthenticateWithSrpAsync(userName, password);

                if (User != null && User.SessionTokens != null)
                {
                    DisplayAccessToken(User.SessionTokens.AccessToken);
                    DisplayOpenIdToken(User.SessionTokens.IdToken);
                    this.SendMessageButton.IsEnabled = true;
                    this.GetMessageButton.IsEnabled = false;
                    this.ListBucketsButton.IsEnabled = true;
                    this.RefreshButton.IsEnabled = true;
                }
            }
        }
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            string userName = this.UserName.Text;
            string refreshToken = null;

            if (userName != null && userName != string.Empty &&
                this.User != null && this.User.SessionTokens != null)
            {
                refreshToken = this.User.SessionTokens.RefreshToken;
                this.AccessTokenText.Text = string.Empty;
                this.OpenIdTokenText.Text = string.Empty;

                AuthFlowResponse authResponse = await GetCredsFromRefreshAsync(userName, refreshToken);

                if (authResponse != null && authResponse.AuthenticationResult != null &&
                    User != null && User.SessionTokens != null)
                {
                    DisplayAccessToken(User.SessionTokens.AccessToken);
                    DisplayOpenIdToken(User.SessionTokens.IdToken);
                }
            }
        }

        private async void ListBuckets_Click(object sender, RoutedEventArgs e)
        {
            var credentials = await GetAuthenticatedCredentials();

            var list = await ListS3Buckets(credentials, RegionEndpoint.EUWest1);

            this.S3List.Text = list;
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=SQS/MSQSSendMessageSendMessageRequest.html
            this.SendMessageButton.IsEnabled = false;
            this.GetMessageButton.IsEnabled = true;

            var credentials = await GetAuthenticatedCredentials();
            await SendMessage(credentials, RegionEndpoint.EUWest1);
        }
        private async void GetMessage_Click(object sender, RoutedEventArgs e)
        {
            //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=SQS/MSQSReceiveMessageReceiveMessageRequest.html
            this.SendMessageButton.IsEnabled = true;
            this.GetMessageButton.IsEnabled = false;

            var credentials = await GetAuthenticatedCredentials();
            await GetMessage(credentials, RegionEndpoint.EUWest1);
        }
        private async Task<AuthFlowResponse> AuthenticateWithSrpAsync(string userName, string password)
        {
            AuthFlowResponse authResponse = null;
            var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), FallbackRegionFactory.GetRegionEndpoint());
            var userPool = new CognitoUserPool(UserPoolId, ClientId, provider);
            this.User = new CognitoUser(userName, ClientId, userPool, provider);

            try
            {
                authResponse = await this.User.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
                {
                    Password = password
                }).ConfigureAwait(false);

                if (authResponse.ChallengeName == ChallengeNameType.NEW_PASSWORD_REQUIRED)
                {
                    authResponse = await this.User.RespondToNewPasswordRequiredAsync(new RespondToNewPasswordRequiredRequest()
                    {
                        SessionID = authResponse.SessionID,
                        NewPassword = "HollandCasino1!"
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return authResponse;
        }

        private async Task<AuthFlowResponse> GetCredsFromRefreshAsync(string userName, string refreshToken)
        {
            AuthFlowResponse authResponse = null; 

            this.User.SessionTokens = new CognitoUserSession(null, null, refreshToken, DateTime.Now, DateTime.Now.AddHours(1));

            try
            {
                InitiateRefreshTokenAuthRequest refreshRequest = new InitiateRefreshTokenAuthRequest()
                {
                    AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
                };

                authResponse = await User.StartWithRefreshTokenAuthAsync(refreshRequest).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                //TODO Show all exceptions!
                Console.WriteLine("!!!Re-login again!!!");
                Console.WriteLine(ex.Message);
            }
            return authResponse;
        }

        private async Task<string> ListS3Buckets(CognitoAWSCredentials creds, RegionEndpoint region)
        {

            StringBuilder sb = new StringBuilder();

            using (var s3Client = new AmazonS3Client(creds, region))
            {
                try
                {
                    ListBucketsResponse response = await s3Client.ListBucketsAsync();

                    foreach (S3Bucket bucket in response.Buckets)
                    {
                        Console.WriteLine("Bucket {0}, Created on {1}", bucket.BucketName, bucket.CreationDate);

                        sb.AppendFormat("{0}{1}", bucket.BucketName, Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return sb.ToString() ;
        }

        private async Task<bool> SendMessage(CognitoAWSCredentials creds, RegionEndpoint region)
        {
            bool result;
            try
            {
                var client = new AmazonSQSClient(creds, region);

                var request = new SendMessageRequest
                {
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                          "TestMessageCount", new MessageAttributeValue
                            { DataType = "Number", StringValue = (++messageCount).ToString() }
                        }
                    },
                    MessageBody = this.MessageTextBox.Text,
                    QueueUrl = this.QueuUrlTextBox.Text
                };

                var response = await client.SendMessageAsync(request);

                Console.WriteLine("For message ID '" + response.MessageId + "':");
                Console.WriteLine("  MD5 of message attributes: " +
                  response.MD5OfMessageAttributes);
                Console.WriteLine("  MD5 of message body: " + response.MD5OfMessageBody);

                result = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                result = false;
            }

            return result;
        }

        private async Task<bool> GetMessage(CognitoAWSCredentials creds, RegionEndpoint region)
        {
            bool result;
            try
            {
                var client = new AmazonSQSClient(creds, region);

                var request = new ReceiveMessageRequest
                {
                    AttributeNames = new List<string>() { "All" },
                    MaxNumberOfMessages = 5,
                    QueueUrl = this.QueuUrlTextBox.Text,
                    VisibilityTimeout = (int)TimeSpan.FromMinutes(10).TotalSeconds,
                    WaitTimeSeconds = (int)TimeSpan.FromSeconds(5).TotalSeconds
                };

                var response = client.ReceiveMessage(request);
                StringBuilder sb = new StringBuilder();

                if (response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {

                        sb.AppendFormat("{0}{1}", message.Body, Environment.NewLine);

                        Console.WriteLine("For message ID '" + message.MessageId + "':");
                        Console.WriteLine("  Body: " + message.Body);
                        Console.WriteLine("  Receipt handle: " + message.ReceiptHandle);
                        Console.WriteLine("  MD5 of body: " + message.MD5OfBody);
                        Console.WriteLine("  MD5 of message attributes: " +
                          message.MD5OfMessageAttributes);
                        Console.WriteLine("  Attributes:");

                        foreach (var attr in message.Attributes)
                        {
                            Console.WriteLine("    " + attr.Key + ": " + attr.Value);
                        }

                        var deleteRequest = new DeleteMessageRequest
                        {
                            QueueUrl = this.QueuUrlTextBox.Text,
                            ReceiptHandle = message.ReceiptHandle
                        };

                        await client.DeleteMessageAsync(deleteRequest);
                    }
                    
                    this.ReceivedMessageTextBlock.Text = sb.ToString();
                }
                else
                {
                    this.ReceivedMessageTextBlock.Text = "No messages received.";
                    Console.WriteLine("No messages received.");
                }
                result = true;
            }
            catch (Exception ex)
            {
                this.ReceivedMessageTextBlock.Text = ex.Message;
                Console.WriteLine(ex.Message);
                result = false;
            }
            return result;
        }

        private void DisplayAccessToken(string accessToken)
        {
            if (accessToken != null &&
                accessToken != string.Empty)
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadToken(accessToken) as JwtSecurityToken;

                StringBuilder sb = new StringBuilder();

                foreach (var claim in token.Claims)
                {
                    sb.AppendFormat("{0}: {1}{2}", claim.Type, claim.Value, Environment.NewLine);
                }

                this.AccessTokenText.Text = sb.ToString();
            }
        }
        private void DisplayOpenIdToken(string idToken)
        {
            if (idToken != null &&
                idToken != string.Empty)
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadToken(idToken) as JwtSecurityToken;

                StringBuilder sb = new StringBuilder();

                foreach (var claim in token.Claims)
                {
                    sb.AppendFormat("{0}: {1}{2}", claim.Type, claim.Value, Environment.NewLine);
                }

                this.OpenIdTokenText.Text = sb.ToString();
            }
        }
    }
}
