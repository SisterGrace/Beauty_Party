namespace TK_Engine
{
    internal class ResourceLogger : YooAsset.ILogger
    {
        public void Log(string message)
        {
            TK_Engine.Log.Info(message);
        }

        public void Warning(string message)
        {
            TK_Engine.Log.Warning(message);
        }

        public void Error(string message)
        {
            TK_Engine.Log.Error(message);
        }

        public void Exception(System.Exception exception)
        {
            TK_Engine.Log.Fatal(exception.Message);
        }
    }
}