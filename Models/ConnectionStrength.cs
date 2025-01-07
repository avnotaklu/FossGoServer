[Immutable, GenerateSerializer]
[Alias("ConnectionStrength")]
public class ConnectionStrength
{
    [Id(0)]
    public int Ping { get; set; }

    static public int Worst => 10_000;

    public ConnectionStrength(int ping)
    {
        Ping = ping;
    }
}