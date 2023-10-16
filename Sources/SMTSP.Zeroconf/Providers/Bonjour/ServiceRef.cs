#region header

// Arkane.ZeroConf - ServiceRef.cs
// 

#endregion

#region using

using System ;
using System.Threading ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf.Providers.Bonjour ;

public struct ServiceRef
{
    public static readonly ServiceRef Zero ;

    public ServiceRef (IntPtr raw) => Raw = raw ;

    public void Deallocate () { Native.DNSServiceRefDeallocate (Raw) ; }

    public ServiceError ProcessSingle () => Native.DNSServiceProcessResult (Raw) ;

    public void Process ()
    {
        Process(CancellationToken.None);
    }

    public void Process (CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (ProcessSingle() == ServiceError.NoError)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public int SocketFD => Native.DNSServiceRefSockFD (Raw) ;

    public IntPtr Raw { get ; }

    public override bool Equals (object o)
    {
        if (!(o is ServiceRef))
            return false ;

        return ((ServiceRef) o).Raw == Raw ;
    }

    public override int GetHashCode () => Raw.GetHashCode () ;

    public static bool operator == (ServiceRef a, ServiceRef b) => a.Raw == b.Raw ;

    public static bool operator != (ServiceRef a, ServiceRef b) => a.Raw != b.Raw ;

    public static explicit operator IntPtr (ServiceRef value) => value.Raw ;

    public static explicit operator ServiceRef (IntPtr value) => new(value) ;
}
