#region header

// Arkane.ZeroConf - BrowseService.cs
// 

#endregion

#region using

using System;
using System.Collections;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

#endregion

namespace SMTSP.Bonjour.Providers.Bonjour ;

public sealed class BrowseService : Service, IResolvableService
{
    public BrowseService () { SetupCallbacks () ; }

    public BrowseService (string name, string replyDomain, string regtype) : base (name, replyDomain, regtype)
    {
        SetupCallbacks () ;
    }

    internal BrowseService (Channel<IResolvableService> channel) : this ()
    {
        this.channel = channel ;
    }

    private readonly Channel<IResolvableService> channel ;

    private Native.DNSServiceQueryRecordReply queryRecordReplyHandler ;

    private Action <bool, CancellationToken> resolveAction ;
    private bool                             resolvePending ;

    private Native.DNSServiceResolveReply resolveReplyHandler ;

    private IAsyncResult resolveResult ;

    public bool IsResolved { get ; private set ; }

    public event ServiceResolvedEventHandler Resolved ;

    public void Resolve ()
    {
        // If people call this in a ServiceAdded event handler (which they generally do), we need to
        // invoke onto another thread, otherwise we block processing any more results.
        resolveResult = ResolveAsync () ;
    }

    public Task ResolveAsync (CancellationToken cancellationToken = default)
    {
        return Task.Run (() => resolveAction (false, cancellationToken), cancellationToken) ;
    }

    ~BrowseService ()
    {
        if (resolveResult != null)
            resolveAction.EndInvoke (resolveResult) ;
    }

    private void SetupCallbacks ()
    {
        resolveReplyHandler     = OnResolveReply ;
        queryRecordReplyHandler = OnQueryRecordReply ;
        resolveAction           = Resolve ;
    }

    public void Resolve (bool requery)
    {
        Resolve (requery, CancellationToken.None) ;
    }

    public void Resolve (bool requery, CancellationToken cancellationToken)
    {
        if (resolvePending)
            return ;

        IsResolved     = false ;
        resolvePending = true ;

        if (requery)
            InterfaceIndex = 0 ;

        var error = Native.DNSServiceResolve (out var sdRef,
                                              ServiceFlags.None,
                                              InterfaceIndex,
                                              Encoding.UTF8.GetBytes (Name),
                                              RegType,
                                              ReplyDomain,
                                              resolveReplyHandler,
                                              IntPtr.Zero) ;

        if (error != ServiceError.NoError)
            throw new ServiceErrorException (error) ;

        sdRef.Process (cancellationToken) ;
    }

    public void RefreshTxtRecord ()
    {
        // Should probably make this async?

        var error = Native.DNSServiceQueryRecord (out var sdRef,
                                                  ServiceFlags.None,
                                                  0,
                                                  fullname,
                                                  ServiceType.TXT,
                                                  ServiceClass.IN,
                                                  queryRecordReplyHandler,
                                                  IntPtr.Zero) ;

        if (error != ServiceError.NoError)
            throw new ServiceErrorException (error) ;

        sdRef.Process () ;
    }

    private void OnResolveReply (ServiceRef   sdRef,
                                 ServiceFlags flags,
                                 uint         interfaceIndex,
                                 ServiceError errorCode,
                                 IntPtr       fullname,
                                 string       hosttarget,
                                 ushort       port,
                                 ushort       txtLen,
                                 IntPtr       txtRecord,
                                 IntPtr       contex)
    {
        IsResolved     = true ;
        resolvePending = false ;

        InterfaceIndex = interfaceIndex ;
        FullName       = Marshal.PtrToStringUTF8 (fullname) ;
        this.port           = (ushort) IPAddress.NetworkToHostOrder ((short) port) ;
        TxtRecord      = new TxtRecord (txtLen, txtRecord) ;
        this.hosttarget     = hosttarget ;

        sdRef.Deallocate () ;

        // Run an A query to resolve the IP address
        ServiceRef sd_ref ;

        if ((AddressProtocol == AddressProtocol.Any) || (AddressProtocol == AddressProtocol.IPv4))
        {
            var error = Native.DNSServiceQueryRecord (out sd_ref,
                                                      ServiceFlags.None,
                                                      interfaceIndex,
                                                      hosttarget,
                                                      ServiceType.A,
                                                      ServiceClass.IN,
                                                      queryRecordReplyHandler,
                                                      IntPtr.Zero) ;

            if (error != ServiceError.NoError)
                throw new ServiceErrorException (error) ;

            sd_ref.Process () ;
        }

        if ((AddressProtocol == AddressProtocol.Any) || (AddressProtocol == AddressProtocol.IPv6))
        {
            var error = Native.DNSServiceQueryRecord (out sd_ref,
                                                      ServiceFlags.None,
                                                      interfaceIndex,
                                                      hosttarget,
                                                      ServiceType.AAAA,
                                                      ServiceClass.IN,
                                                      queryRecordReplyHandler,
                                                      IntPtr.Zero) ;

            if (error != ServiceError.NoError)
                throw new ServiceErrorException (error) ;

            sd_ref.Process () ;
        }

        if (hostentry.AddressList != null)
        {
            var handler = Resolved ;
            handler?.Invoke (this, new ServiceResolvedEventArgs (this)) ;
            channel?.Writer.TryWrite (this) ;
        }
    }

    private void OnQueryRecordReply (ServiceRef   sdRef,
                                     ServiceFlags flags,
                                     uint         interfaceIndex,
                                     ServiceError errorCode,
                                     string       fullname,
                                     ServiceType  rrtype,
                                     ServiceClass rrclass,
                                     ushort       rdlen,
                                     IntPtr       rdata,
                                     uint         ttl,
                                     IntPtr       context)
    {
        switch (rrtype)
        {
            case ServiceType.A:
            case ServiceType.AAAA:
                IPAddress address ;

                if (rdlen == 4)
                {
                    // ~4.5 times faster than Marshal.Copy into byte[4]
                    var addressRaw = (uint) (Marshal.ReadByte (rdata, 3) << 24) ;
                    addressRaw |= (uint) (Marshal.ReadByte (rdata, 2) << 16) ;
                    addressRaw |= (uint) (Marshal.ReadByte (rdata, 1) << 8) ;
                    addressRaw |= Marshal.ReadByte (rdata, 0) ;

                    address = new IPAddress (addressRaw) ;
                }
                else if (rdlen == 16)
                {
                    var addressRaw = new byte[rdlen] ;
                    Marshal.Copy (rdata, addressRaw, 0, rdlen) ;
                    address = new IPAddress (addressRaw, interfaceIndex) ;
                }
                else
                {
                    break ;
                }

                if (hostentry == null)
                    hostentry = new IPHostEntry { HostName = hosttarget } ;

                if (hostentry.AddressList != null)
                {
                    var list = new ArrayList (hostentry.AddressList) { address } ;
                    hostentry.AddressList = list.ToArray (typeof (IPAddress)) as IPAddress[] ;
                }
                else
                {
                    hostentry.AddressList = new[] { address } ;
                }

                //ServiceResolvedEventHandler handler = this.Resolved ;
                //if (handler != null)
                //    handler (this, new ServiceResolvedEventArgs (this)) ;

                break ;
            case ServiceType.TXT:
                TxtRecord?.Dispose () ;

                TxtRecord = new TxtRecord (rdlen, rdata) ;
                break ;
        }

        if ((flags & ServiceFlags.MoreComing) != ServiceFlags.MoreComing)
            sdRef.Deallocate () ;
    }
}
