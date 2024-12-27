[Immutable, GenerateSerializer]
[Alias("ConnectionStrength")]
public class ConnectionStrength
{
    [Id(0)]
    public int Ping { get; set; }

    public ConnectionStrength(int ping)
    {
        Ping = ping;
    }
}