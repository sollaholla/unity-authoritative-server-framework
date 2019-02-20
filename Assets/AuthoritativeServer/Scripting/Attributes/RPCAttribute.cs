using System;

namespace AuthoritativeServer.Attributes
{
    /// <summary>
    /// A method attribute to identify remote procedure calls.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NetworkRPCAttribute : Attribute { }
}
