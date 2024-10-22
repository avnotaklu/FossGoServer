namespace BadukServer.Dto;

public class RegisterPlayerDto
{
    public RegisterPlayerDto(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public string ConnectionId { get; set; }
}