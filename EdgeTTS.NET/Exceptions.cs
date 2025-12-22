namespace EdgeTTS.NET;

public class EdgeTTSException : Exception
{
    public EdgeTTSException(string message) : base(message) { }
    public EdgeTTSException(string message, Exception innerException) : base(message, innerException) { }
}

public class NoAudioReceivedException : EdgeTTSException
{
    public NoAudioReceivedException(string message) : base(message) { }
}

public class UnexpectedResponseException : EdgeTTSException
{
    public UnexpectedResponseException(string message) : base(message) { }
}

public class UnknownResponseException : EdgeTTSException
{
    public UnknownResponseException(string message) : base(message) { }
}

public class WebSocketException : EdgeTTSException
{
    public WebSocketException(string message) : base(message) { }
    public WebSocketException(string message, Exception innerException) : base(message, innerException) { }
}
