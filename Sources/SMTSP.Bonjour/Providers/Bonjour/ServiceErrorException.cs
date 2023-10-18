#region header

// Arkane.ZeroConf - ServiceErrorException.cs
// 

#endregion

#region using

using System;

#endregion

namespace SMTSP.Bonjour.Providers.Bonjour ;

internal class ServiceErrorException : Exception
{
    internal ServiceErrorException (ServiceError error) : base (error.ToString ()) { }

    internal ServiceErrorException (string error) : base (error) { }
}
