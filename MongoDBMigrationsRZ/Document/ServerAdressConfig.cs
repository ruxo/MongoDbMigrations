using System;

namespace MongoDBMigrations.Document;

public class ServerAdressConfig
{
    private string _host = string.Empty;
    private uint _port;

    public string Host
    {
        get => _host;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            _host = value;
        }
    }

    public uint Port
    {
        get => _port;
        set
        {
            if (value > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "Port number must greater than 65535");

            _port = value;
        }
    }

    public int PortAsInt => (int)_port;
}