using System;

namespace DalCore.DatabaseAccess
{
    [Flags]
    public enum DefaulDatabaseOperation
    {
        None = 0,
        Create = 1,
        Read = 2,
        Update = 4,
        Delete = 8,
        Search = 16,
        Get = 32,
        Save = Update | Create | Delete
    }
}
