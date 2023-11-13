using System;

namespace MongoDBMigrations.Document
{
    public class ServerAdressConfig
    {
        private string _host;
        private uint _port = 0;

        public string Host
        {
            get { return _host; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(value));

                _host = value;
            }
        }

        public uint Port
        {
            get { return _port; }
            set
            {
                if (value > 65535)
                    throw new ArgumentOutOfRangeException("Port number must greater than 65535");

                _port = value;
            }
        }

        public int PortAsInt
        {
            get { return (int)_port; }
        }
    }
}
