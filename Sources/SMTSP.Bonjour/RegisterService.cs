#region header

// Arkane.ZeroConf - RegisterService.cs
// 

#endregion

#region using

using System;
using SMTSP.Bonjour.Providers;

#endregion

namespace SMTSP.Bonjour ;

public class RegisterService : IRegisterService
{
    public RegisterService () => registerService =
                                     (IRegisterService) Activator.CreateInstance (ProviderFactory
                                                                                 .SelectedProvider.RegisterService) ;

    private readonly IRegisterService registerService ;

    public string Name { get => registerService.Name ; set => registerService.Name = value ; }

    public string RegType { get => registerService.RegType ; set => registerService.RegType = value ; }

    public string ReplyDomain { get => registerService.ReplyDomain ; set => registerService.ReplyDomain = value ; }

    public ITxtRecord TxtRecord { get => registerService.TxtRecord ; set => registerService.TxtRecord = value ; }

    public short Port { get => registerService.Port ; set => registerService.Port = value ; }

    public ushort UPort { get => registerService.UPort ; set => registerService.UPort = value ; }

    public void Register () { registerService.Register () ; }

    public void Dispose () { registerService.Dispose () ; }

    public event RegisterServiceEventHandler Response
    {
        add => registerService.Response += value ;
        remove => registerService.Response -= value ;
    }
}
