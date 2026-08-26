using System.Collections.ObjectModel;

namespace SmartPOS.Application.Extensions;

public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Synchronizes the ObservableCollection with a new set of items.
    /// This prevents the need to recreate the collection, which can break WPF bindings.
    /// </summary>
    public static void SyncWith<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (newItems == null) throw new ArgumentNullException(nameof(newItems));

        collection.Clear();
        foreach (var item in newItems)
        {
            collection.Add(item);
        }
    }
}
