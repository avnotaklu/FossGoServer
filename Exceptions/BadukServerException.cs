public class BadukServerException : Exception
{
    public string message;
    public string data;
    public BadukServerException( string message,string data)
    {
        this.message = message;
        this.data = data;
    }

    public override string ToString()
    {
        return $" Message: {message}\n Data: {data}";
    }
}