using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;
public static class EnumerableExtensions
{
    /// <summary>
    /// Disposes all items in the sequence, i.e., releases unmanaged resources associated with the items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="seq"></param>
    public static void DisposeAll<T>(this IEnumerable<T> seq) where T : IDisposable
    {
        foreach (T item in seq)
            item?.Dispose();
    }
}
