#region header

// Arkane.ZeroConf - ServiceBrowseEventArgs.cs
// 

#endregion

#region using

using System;

#endregion

namespace SMTSP.Bonjour ;

public class ServiceBrowseEventArgs : EventArgs
{
    public ServiceBrowseEventArgs (IResolvableService service) => Service = service ;

    public IResolvableService Service { get ; }
}
