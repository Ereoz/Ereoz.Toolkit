namespace Ereoz.Toolkit
{
    public class UpdateInfo
    {
        public string Message { get; private set; }

        public UpdateInfo(string message)
        {
            Message = message;
        }
    }
}
