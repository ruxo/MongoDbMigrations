using System;

namespace MongoDBMigrations.Core
{
    public class ConfirmationEventArgs : EventArgs
    {
        public string Question { get; set; }
        public bool Continue { get; set; }
    }
}
