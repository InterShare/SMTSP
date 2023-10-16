#region header

// Arkane.ZeroConf - TxtRecord.cs
// 

#endregion

#region using

using System ;
using System.Collections ;

using ArkaneSystems.Arkane.Zeroconf.Providers ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf ;

public class TxtRecord : ITxtRecord
{
    public TxtRecord () => BaseRecord = (ITxtRecord) Activator.CreateInstance (ProviderFactory.SelectedProvider.TxtRecord) ;

    public TxtRecordItem this [string index] => BaseRecord[index] ;

    public int Count => BaseRecord.Count ;

    public ITxtRecord BaseRecord { get ; }

    public void Add (string key, string value) { BaseRecord.Add (key, value) ; }

    public void Add (string key, byte[] value) { BaseRecord.Add (key, value) ; }

    public void Add (TxtRecordItem item) { BaseRecord.Add (item) ; }

    public void Remove (string key) { BaseRecord.Remove (key) ; }

    public TxtRecordItem GetItemAt (int index) => BaseRecord.GetItemAt (index) ;

    public IEnumerator GetEnumerator () => BaseRecord.GetEnumerator () ;

    public void Dispose () { BaseRecord.Dispose () ; }
}
