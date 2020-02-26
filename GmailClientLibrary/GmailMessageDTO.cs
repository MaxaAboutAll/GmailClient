namespace GmailClientLibrary
{
    public class GmailMessageDTO
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
        public string Snippet { get; set; }
        
        public override string ToString()
        {
            return $"Id: {Id}\nFrom: {From}\nTo: {To}\nMessage: {Message}\nDate: {Date}\nSnippet: {Snippet}\n";
        }
    }
}