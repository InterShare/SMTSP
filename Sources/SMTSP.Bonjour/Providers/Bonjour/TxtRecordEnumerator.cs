#region header

// Arkane.ZeroConf - TxtRecordEnumerator.cs
// 

#endregion

#region using

using System.Collections;

#endregion

namespace SMTSP.Bonjour.Providers.Bonjour ;

internal class TxtRecordEnumerator : IEnumerator
{
    public TxtRecordEnumerator (TxtRecord record) => this.record = record ;

    private readonly TxtRecord record ;

    private TxtRecordItem currentItem ;
    private int           index ;

    public object Current => currentItem ;

    public void Reset ()
    {
        index       = 0 ;
        currentItem = null ;
    }

    public bool MoveNext ()
    {
        if ((index < 0) || (index >= record.Count))
            return false ;

        currentItem = record.GetItemAt (index++) ;
        return currentItem != null ;
    }
}
