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
    public ServiceBrowseEventArgs (BrowseService service, bool moreComing) : base (service) => this.MoreComing = moreComing ;

    public bool MoreComing { get ; }
}

public class ServiceBrowser : IServiceBrowser, IDisposable
{
    public ServiceBrowser () => this.browseReplyHandler = this.OnBrowseReply ;

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
        this.Configure (interfaceIndex, addressProtocol, regtype, domain) ;
        this.StartAsync () ;
    }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync(uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelOptions = new UnboundedChannelOptions { SingleReader = true } ;
        channel = Channel.CreateUnbounded<IResolvableService> (channelOptions) ;

        this.Configure (interfaceIndex, addressProtocol, regtype, domain) ;
        this.StartAsync () ;

        await foreach (var result in channel.Reader.ReadAllAsync (cancellationToken))
        {
            yield return result ;
        }
    }

    public void Dispose () { this.Stop () ; }

    public IEnumerator <IResolvableService> GetEnumerator ()
    {
        serviceTableSemaphore.Wait();
        try
        {
            foreach (var service in this.serviceTable.Values)
                yield return service ;
        }
        finally
        {
            serviceTableSemaphore.Release();
        }
    }

    IEnumerator IEnumerable.GetEnumerator () => this.GetEnumerator () ;

    public void Configure (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
    {
        this.interfaceIndex   = interfaceIndex ;
        this.address_protocol = addressProtocol ;
        this.regtype          = regtype ;
        this.domain           = domain ;

        if (regtype == null)
            throw new ArgumentNullException ("regtype") ;
    }

    private void Start (bool async)
    {
        if (this.task != null)
            throw new InvalidOperationException ("ServiceBrowser is already started") ;

        if (async)
            this.task = Task.Run (() => this.ProcessStart ())
                            .ContinueWith (_ =>
                                           {
                                               this.task = null ;
                                               if (_.IsFaulted)
                                               {
                                                   Debug.Assert (_.Exception != null, "_.Exception != null") ;
                                                   throw _.Exception ;
                                               }
                                           }) ;
        else
            this.ProcessStart () ;
    }

    public void Start () { this.Start (false) ; }

    public void StartAsync () { this.Start (true) ; }

    private void ProcessStart ()
    {
        var error = Native.DNSServiceBrowse (out this.sdRef,
                                             ServiceFlags.None,
                                             this.interfaceIndex,
                                             this.regtype,
                                             this.domain,
                                             this.browseReplyHandler,
                                             IntPtr.Zero) ;

        if (error != ServiceError.NoError)
            throw new ServiceErrorException (error) ;

        this.sdRef.Process () ;
    }

    public void Stop ()
    {
        if (this.sdRef != ServiceRef.Zero)
        {
            this.sdRef.Deallocate () ;
            this.sdRef = ServiceRef.Zero ;
        }

        this.task?.Wait () ;
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
                          AddressProtocol = this.address_protocol,
                      } ;

        var args = new ServiceBrowseEventArgs (
                                               service,
                                               (flags & ServiceFlags.MoreComing) != 0) ;

        if ((flags & ServiceFlags.Add) != 0)
        {
            serviceTableSemaphore.Wait();
            try
            {
                this.serviceTable[name] = service;
            }
            finally
            {
                serviceTableSemaphore.Release();
            }

            var handler = this.ServiceAdded ;
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
                this.serviceTable.Remove (name) ;
            }
            finally
            {
                serviceTableSemaphore.Release();
            }

            var handler = this.ServiceRemoved ;
            handler?.Invoke (this, args) ;
        }
    }
}
