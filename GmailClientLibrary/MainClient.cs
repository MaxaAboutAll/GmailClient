using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;

namespace GmailClientLibrary
{
    public class MainClient
    {
        private static string[] Scopes = { GmailService.Scope.GmailReadonly };
        private string ApplicationName = "Gmail Client";
        private readonly GmailService service;
        
        public MainClient()
        {
            UserCredential credential;
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            } 
            
            service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public IEnumerable<GmailMessageDTO> GetMyMails()
        {
            var messageIds = GetListMessageIds();
            foreach (var e in messageIds)
            {
                var gettingMessage = service.Users.Messages.Get("me", e.Id);
                gettingMessage.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                var myMes = gettingMessage.Execute();
                var message = service.Users.Messages.Get("me", e.Id).Execute();
                var from = message.Payload.Headers.First(x => x.Name.Equals("From")).Value;
                var to = message.Payload.Headers.First(x => x.Name.Equals("To")).Value;
                var date = message.Payload.Headers.First(x => x.Name.Equals("Date")).Value;
                var messageText = message.Payload.Parts?.FirstOrDefault(x => x.MimeType.Equals("text/plain"))?.Body.Data;
                yield return new GmailMessageDTO{From = from, To = to, Date = date, 
                    Id = message.Id, Message = Base64Decode(messageText), Snippet = message.Snippet};
            }
        } 
        
        private IList<Message> GetListMessageIds()
        {
            var request = service.Users.Messages.List("me");
             request.LabelIds = new Repeatable<string>(new []{"INBOX"});
            return request.Execute().Messages;
        }
        
        private string Base64Encode(string plainText) {
            if(plainText == null)
                throw new ArgumentException();
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        
        private string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData == null)
                return "";
            var newMas = base64EncodedData.Replace('-', '+').Replace('_', '/');
            string myStr = "";
            foreach (var e in newMas)
                myStr += e;
            var base64EncodedBytes = System.Convert.FromBase64String(myStr);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}