namespace eProject.Helpers
{
    public class APIRespone
    {
        public int Status { get; }
        public string Message { get; }
        public Object? Data {  get; }

        public APIRespone(int status, string message, object? data)
        {
            Status = status;
            Message = message;
            Data = data;
        }
    }
}
