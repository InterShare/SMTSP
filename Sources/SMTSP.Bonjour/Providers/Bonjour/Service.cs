#region header

// Arkane.ZeroConf - Service.cs
// 

#endregion

#region using

using System.Net;

#endregion

namespace SMTSP.Bonjour.Providers.Bonjour ;

public abstract class Service : IService
{
    public Service () { }

    public Service (string name, string replyDomain, string regtype)
    {
        Name        = name ;
        ReplyDomain = replyDomain ;
        RegType     = regtype ;
    }

    protected AddressProtocol address_protocol ;
    protected ServiceFlags    flags = ServiceFlags.None ;
    protected string          fullname ;
    protected IPHostEntry     hostentry ;
    protected string          hosttarget ;
    protected uint            interface_index ;
    protected string          name ;
    protected ushort          port ;
    protected string          regtype ;
    protected string          reply_domain ;

    protected ITxtRecord txt_record ;

    public ServiceFlags Flags { get => flags ; internal set => flags = value ; }

    public uint InterfaceIndex { get => interface_index ; set => interface_index = value ; }

    public AddressProtocol AddressProtocol { get => address_protocol ; set => address_protocol = value ; }

    public string Name { get => name ; set => name = value ; }

    public string ReplyDomain { get => reply_domain ; set => reply_domain = value ; }

    public string RegType { get => regtype ; set => regtype = value ; }

    // Resolved Properties

    public ITxtRecord TxtRecord { get => txt_record ; set => txt_record = value ; }

    public string FullName { get => fullname ; internal set => fullname = value ; }

    public string HostTarget => hosttarget ;

    public IPHostEntry HostEntry => hostentry ;

    public uint NetworkInterface => interface_index ;

    public short Port { get => (short) UPort ; set => UPort = (ushort) value ; }

    public ushort UPort { get => port ; set => port = value ; }

    public override bool Equals (object o)
    {
        if (!(o is Service))
            return false ;

        return ((Service) o).Name == Name ;
    }

    public override int GetHashCode () => Name.GetHashCode () ;
}
