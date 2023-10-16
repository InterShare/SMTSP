#region header

// Arkane.ZeroConf - RegisterService.cs
// 

#endregion

#region using

using System ;
using System.Net ;
using System.Runtime.InteropServices ;
using System.Text ;
using System.Threading ;
using System.Threading.Tasks ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf.Providers.Bonjour ;

public sealed class RegisterService : Service, IRegisterService, IDisposable
{
    public RegisterService () { SetupCallback () ; }

    public RegisterService (string name, string replyDomain, string regtype) : base (name, replyDomain, regtype)
    {
        SetupCallback () ;
    }

    private readonly CancellationTokenSource cts = new() ;

    private Native.DNSServiceRegisterReply registerReplyHandler ;
    private ServiceRef                     sdRef ;
    private Task                           task ;

    public bool AutoRename { get ; set ; } = true ;

    public event RegisterServiceEventHandler Response ;

    public void Register () { Register (true) ; }

    public void Dispose ()
    {
        cts?.Cancel () ;

        cts.Dispose () ;
        sdRef.Deallocate () ;
    }

    private void SetupCallback () { registerReplyHandler = OnRegisterReply ; }

    public void Register (bool async)
    {
        if (task != null)
            throw new InvalidOperationException ("RegisterService registration already in process") ;

        if (async)
            task = Task.Run (() => ProcessRegister ()).ContinueWith (_ => task = null, cts.Token) ;
        else
            ProcessRegister () ;
    }

    public void RegisterSync () { Register (false) ; }

    public void ProcessRegister ()
    {
        ushort txtRecLength = 0 ;
        byte[] txtRec       = null ;

        if (TxtRecord != null)
        {
            txtRecLength = ((TxtRecord) TxtRecord.BaseRecord).RawLength ;
            txtRec       = new byte[txtRecLength] ;
            Marshal.Copy (((TxtRecord) TxtRecord.BaseRecord).RawBytes, txtRec, 0, txtRecLength) ;
        }

        var error = Native.DNSServiceRegister (out sdRef,
                                               AutoRename ? ServiceFlags.None : ServiceFlags.NoAutoRename,
                                               InterfaceIndex,
                                               Encoding.UTF8.GetBytes (Name),
                                               RegType,
                                               ReplyDomain,
                                               HostTarget,
                                               (ushort) IPAddress.HostToNetworkOrder ((short) port),
                                               txtRecLength,
                                               txtRec,
                                               registerReplyHandler,
                                               IntPtr.Zero) ;

        if (error != ServiceError.NoError)
            throw new ServiceErrorException (error) ;

        sdRef.Process () ;
    }

    private void OnRegisterReply (ServiceRef   sdRef,
                                  ServiceFlags flags,
                                  ServiceError errorCode,
                                  IntPtr       name,
                                  string       regtype,
                                  string       domain,
                                  IntPtr       context)
    {
        var args = new RegisterServiceEventArgs
                   {
                       Service = this, IsRegistered = false, ServiceError = (ServiceErrorCode) errorCode,
                   } ;


        if (errorCode == ServiceError.NoError)
        {
            Name         = Marshal.PtrToStringUTF8(name) ;
            RegType      = regtype ;
            ReplyDomain  = domain ;
            args.IsRegistered = true ;
        }

        var handler = Response ;
        handler?.Invoke (this, args) ;
    }
}
