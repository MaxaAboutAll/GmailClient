using System;
using GmailClientLibrary;

namespace ConsoleForTests
{
    class Program
    {
        static void Main(string[] args)
        {
            MainClient client = new MainClient();
            // var mails = client.GetMyMails();
            // foreach (var mail in mails)
            //     Console.WriteLine(mail);
            client.SendMessage("scryptan@gmail.com", "Test", "Hehe");
        }
    }
}