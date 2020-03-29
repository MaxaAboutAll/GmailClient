using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json.Serialization;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace GmailClientLibrary
{
    public class MainClient
    {
        private static string[] Scopes = { GmailService.Scope.MailGoogleCom };
        private const string ApplicationName = "Gmail Client";
        private const string FileUri = "./messages.json";
        private readonly GmailService service;
        private const string CredPath = "./token.json";
        public static bool isAuthorized => Directory.Exists(CredPath);

        public MainClient()
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(CredPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + CredPath);
            } 
            
            service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public IEnumerable<GmailMessageDTO> GetMyMails(bool isHtml)
        {
            var messagesForSave = new List<GmailMessageDTO>();
            var messageIds = GetListMessageIds();
            foreach (var e in messageIds)
            {
                var message = service.Users.Messages.Get("me", e.Id).Execute();
                var from = message.Payload.Headers.First(x => x.Name.Equals("From")).Value;
                var to = message.Payload.Headers.First(x => x.Name.Equals("To")).Value;
                var date = message.Payload.Headers.First(x => x.Name.Equals("Date")).Value;
                var messageText = message.Payload.Parts?.FirstOrDefault(x => x.MimeType.Equals("text/plain"))?.Body.Data;
                if(isHtml)
                    messageText = message.Payload.Parts?.FirstOrDefault(x => x.MimeType.Equals("text/html"))?.Body.Data;
                var completedMessage = new GmailMessageDTO
                {
                    From = from, To = to, Date = date,
                    Id = message.Id, Message = Base64Decode(messageText), Snippet = message.Snippet
                };
                if (completedMessage.Message.Length > 0)
                {
                    messagesForSave.Add(completedMessage);
                    yield return completedMessage;
                }
            }
            SaveMessages(messagesForSave);
        }

        public void LogOut()
        {
            Directory.Delete(CredPath, true);
        }

        public void SendMessage(string to, string subject, string text)
        {
            var newMsg = new Message {Raw = Base64Encode(CreateMessage(to, subject, text))};
            service.Users.Messages.Send(newMsg, "me").Execute();
        }

        public void DeleteMessage(string messageId)
        {
            service.Users.Messages.Delete("me", messageId).Execute();
        }

        public List<GmailMessageDTO> GetCachedMessages()
        {
            var jsonMail = File.ReadAllText(FileUri);
            return JsonConvert.DeserializeObject<List<GmailMessageDTO>>(jsonMail);
        }
        private void SaveMessages(List<GmailMessageDTO> mail)
        {
            var content = JsonConvert.SerializeObject(mail);
            File.WriteAllText(FileUri, content);
        }
        private IList<Message> GetListMessageIds()
        {
            var request = service.Users.Messages.List("me");
            request.LabelIds = new Repeatable<string>(new []{"INBOX"});
            return request.Execute().Messages;
        }

        private string CreateMessage(string to, string subject, string text)
        {
            return $"To:{to}\r\n" +
                               $"Subject: {subject}\r\n" +
                               "Content-Type: text/plain; charset=us-ascii\r\n\r\n" +
                               $"{text}";
        }
        
        private string Base64Encode(string input) {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
        
        private string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData == null)
                return "";
            var newMas = base64EncodedData.Replace('-', '+').Replace('_', '/');
            string myStr = "";
            foreach (var e in newMas)
                myStr += e;
            var base64EncodedBytes = Convert.FromBase64String(myStr);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}