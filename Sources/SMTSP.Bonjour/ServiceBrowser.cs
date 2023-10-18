#region header

// Arkane.ZeroConf - ServiceBrowser.cs
// 

#endregion

#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using SMTSP.Bonjour.Providers;

#endregion

namespace SMTSP.Bonjour ;

public class ServiceBrowser : IServiceBrowser
{
    public ServiceBrowser () =>
        browser = (IServiceBrowser) Activator.CreateInstance (ProviderFactory.SelectedProvider.ServiceBrowser) ;

    private readonly IServiceBrowser browser ;

    public void Dispose () { browser.Dispose () ; }

    public void Browse (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
    {
        browser.Browse (interfaceIndex, addressProtocol, regtype, domain ?? "local") ;
    }

    public IEnumerator <IResolvableService> GetEnumerator () => browser.GetEnumerator () ;

    IEnumerator IEnumerable.GetEnumerator () => browser.GetEnumerator () ;

    public event ServiceBrowseEventHandler ServiceAdded
    {
        add => browser.ServiceAdded += value ;
        remove => browser.ServiceRemoved -= value ;
    }

    public event ServiceBrowseEventHandler ServiceRemoved
    {
        add => browser.ServiceRemoved += value ;
        remove => browser.ServiceRemoved -= value ;
    }

    public void Browse (uint interfaceIndex, string regtype, string domain)
    {
        Browse (interfaceIndex, AddressProtocol.Any, regtype, domain) ;
    }

    public void Browse (AddressProtocol addressProtocol, string regtype, string domain)
    {
        Browse (0, addressProtocol, regtype, domain) ;
    }

    public void Browse (string regtype, string domain) { Browse (0, AddressProtocol.Any, regtype, domain) ; }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var service in browser.BrowseAsync (interfaceIndex, addressProtocol, regtype, domain ?? "local", cancellationToken))
        {
            yield return service;
        }
    }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync (uint interfaceIndex, string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var service in BrowseAsync (interfaceIndex, AddressProtocol.Any, regtype, domain, cancellationToken))
        {
            yield return service;
        }
    }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync (AddressProtocol addressProtocol, string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var service in BrowseAsync (0, addressProtocol, regtype, domain, cancellationToken))
        {
            yield return service;
        }
    }

    public async IAsyncEnumerable<IResolvableService> BrowseAsync(string regtype, string domain, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var service in BrowseAsync (0, AddressProtocol.Any, regtype, domain, cancellationToken))
        {
            yield return service;
        }
    }
}
