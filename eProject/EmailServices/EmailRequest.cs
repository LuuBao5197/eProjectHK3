namespace eProject.EmailServices
{
    public class EmailRequest
    {
        public string ToMail { get; set; }
        public string Subject { get; set; }
        public string HtmlContent { get; set; }
    }
}
