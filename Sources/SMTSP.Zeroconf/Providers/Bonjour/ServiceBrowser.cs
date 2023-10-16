#region header

// Arkane.ZeroConf - ServiceBrowser.cs
// 

#endregion

#region using

using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Diagnostics ;
using System.Runtime.CompilerServices ;
using System.Runtime.InteropServices ;
using System.Threading ;
using System.Threading.Channels ;
using System.Threading.Tasks ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf.Providers.Bonjour ;

public class ServiceBrowseEventArgs : Arkane.Zeroconf.ServiceBrowseEventArgs
{
    public ServiceBrowseEventArgs (BrowseService service, bool moreComing) : base (service) => MoreComing = moreComing ;

    public bool MoreComing { get ; }
}

public class ServiceBrowser : IServiceBrowser, IDisposable
{
    public ServiceBrowser () => browseReplyHandler = OnBrowseReply ;

    private readonly Native.DNSServiceBrowseReply            browseReplyHandler ;
    private readonly Dictionary <string, IResolvableService> serviceTable = new() ;
    private readonly SemaphoreSlim serviceTableSemaphore = new(1, 1) ;

    private AddressProtocol address_protocol ;
    private string          domain ;
    private uint            interfaceIndex ;
    private string          regtype ;

    private ServiceRef sdRef = ServiceRef.Zero ;

    private Task task ;
    private Channel<IResolvableService> channel ;

    public event ServiceBrowseEventHandler ServiceAdded ;

    public event ServiceBrowseEventHandler ServiceRemoved ;

    public void Browse (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
    {
        Configure (interfaceIndex, addressProtocol, regtype, domain) ;
        StartAsync () ;
    }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync(uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelOptions = new UnboundedChannelOptions { SingleReader = true } ;
        channel = Channel.CreateUnbounded<IResolvableService> (channelOptions) ;

        Configure (interfaceIndex, addressProtocol, regtype, domain) ;
        StartAsync () ;

        await foreach (var result in channel.Reader.ReadAllAsync (cancellationToken))
        {
            yield return result ;
        }
    }

    public void Dispose () { Stop () ; }

    public IEnumerator <IResolvableService> GetEnumerator ()
    {
        serviceTableSemaphore.Wait();
        try
        {
            foreach (var service in serviceTable.Values)
                yield return service ;
        }
        finally
        {
            serviceTableSemaphore.Release();
        }
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator () ;

    public void Configure (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
    {
        this.interfaceIndex   = interfaceIndex ;
        address_protocol = addressProtocol ;
        this.regtype          = regtype ;
        this.domain           = domain ;

        if (regtype == null)
            throw new ArgumentNullException ("regtype") ;
    }

    private void Start (bool async)
    {
        if (task != null)
            throw new InvalidOperationException ("ServiceBrowser is already started") ;

        if (async)
            task = Task.Run (() => ProcessStart ())
                            .ContinueWith (_ =>
                                           {
                                               task = null ;
                                               if (_.IsFaulted)
                                               {
                                                   Debug.Assert (_.Exception != null, "_.Exception != null") ;
                                                   throw _.Exception ;
                                               }
                                           }) ;
        else
            ProcessStart () ;
    }

    public void Start () { Start (false) ; }

    public void StartAsync () { Start (true) ; }

    private void ProcessStart ()
    {
        var error = Native.DNSServiceBrowse (out sdRef,
                                             ServiceFlags.None,
                                             interfaceIndex,
                                             regtype,
                                             domain,
                                             browseReplyHandler,
                                             IntPtr.Zero) ;

        if (error != ServiceError.NoError)
            throw new ServiceErrorException (error) ;

        sdRef.Process () ;
    }

    public void Stop ()
    {
        if (sdRef != ServiceRef.Zero)
        {
            sdRef.Deallocate () ;
            sdRef = ServiceRef.Zero ;
        }

        task?.Wait () ;
    }

    private void OnBrowseReply (ServiceRef   sdRef,
                                ServiceFlags flags,
                                uint         interfaceIndex,
                                ServiceError errorCode,
                                IntPtr       serviceName,
                                string       regtype,
                                string       replyDomain,
                                IntPtr       context)
    {
        var name = Marshal.PtrToStringUTF8 (serviceName) ;

        var service = new BrowseService (channel)
                      {
                          Flags           = flags,
                          Name            = name,
                          RegType         = regtype,
                          ReplyDomain     = replyDomain,
                          InterfaceIndex  = interfaceIndex,
                          AddressProtocol = address_protocol,
                      } ;

        var args = new ServiceBrowseEventArgs (
                                               service,
                                               (flags & ServiceFlags.MoreComing) != 0) ;

        if ((flags & ServiceFlags.Add) != 0)
        {
            serviceTableSemaphore.Wait();
            try
            {
                serviceTable[name] = service;
            }
            finally
            {
                serviceTableSemaphore.Release();
            }

            var handler = ServiceAdded ;
            handler?.Invoke (this, args) ;
            if (channel != null)
            {
                service.ResolveAsync() ;
            }
        }
        else
        {
            serviceTableSemaphore.Wait();
            try
            {
                serviceTable.Remove (name) ;
            }
            finally
            {
                serviceTableSemaphore.Release();
            }

            var handler = ServiceRemoved ;
            handler?.Invoke (this, args) ;
        }
    }
}
