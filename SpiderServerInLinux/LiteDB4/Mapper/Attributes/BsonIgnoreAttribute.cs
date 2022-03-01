using System;

namespace LiteDB4
{
    /// <summary>
    /// Indicate that property will not be persist in Bson serialization
    /// </summary>
    public class BsonIgnoreAttribute : Attribute
    {
    }
}