using System;

namespace AuthoritativeServer.Attributes
{
    /// <summary>
    /// An RPC that will be executed on all clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RPCAttribute : Attribute { }
}
