using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Definition of a <see cref="IReadOnlyCollection{T}"/> that is observable through <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
    /// It has no properties nor methods by itself: it is only here to federate its 3 base interfaces.
    /// This interface is "compatible" with the standard .Net <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>: a specialization of ObservableCollection 
    /// that supports this interface does not need any extra code to be exposed as a true read only observable collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface IObservableReadOnlyCollection<out T> : IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }

}
