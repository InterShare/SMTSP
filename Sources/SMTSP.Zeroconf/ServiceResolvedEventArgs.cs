#region header

// Arkane.ZeroConf - ServiceResolvedEventArgs.cs
// 

#endregion

#region using

using System ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf ;

public class ServiceResolvedEventArgs : EventArgs
{
    public ServiceResolvedEventArgs (IResolvableService service) => Service = service ;

    public IResolvableService Service { get ; }
}
