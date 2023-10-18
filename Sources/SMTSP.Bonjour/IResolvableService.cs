#region header

// Arkane.ZeroConf - IResolvableService.cs
// 

#endregion

#region using

using System.Net;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SMTSP.Bonjour ;

public interface IResolvableService : IService
{
    string FullName { get ; }

    IPHostEntry HostEntry { get ; }

    string HostTarget { get ; }

    uint NetworkInterface { get ; }

    AddressProtocol AddressProtocol { get ; }

    short Port { get ; }

    event ServiceResolvedEventHandler Resolved ;

    void Resolve () ;

    Task ResolveAsync (CancellationToken cancellationToken = default) ;
}
