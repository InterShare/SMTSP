#region header

// Arkane.ZeroConf - RegisterServiceEventArgs.cs
// 

#endregion

#region using

using System ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf ;

public class RegisterServiceEventArgs : EventArgs
{
    public RegisterServiceEventArgs () { }

    public RegisterServiceEventArgs (IRegisterService service, bool isRegistered, ServiceErrorCode error)
    {
        Service      = service ;
        IsRegistered = isRegistered ;
        ServiceError = error ;
    }

    public IRegisterService Service { get ; set ; }

    public bool IsRegistered { get ; set ; }

    public ServiceErrorCode ServiceError { get ; set ; }
}
