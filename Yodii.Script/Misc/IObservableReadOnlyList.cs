using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Definition of an <see cref="IObservableReadOnlyCollection{T}"/> that is <see cref="IReadOnlyList{T}"/> (the index of the elements makes sense).
    /// This interface is "compatible" with the standard .Net <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>: a specialization of ObservableCollection 
    /// that supports this interface does not need any extra code to be exposed as a true read only observable list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public interface IObservableReadOnlyList<out T> : IObservableReadOnlyCollection<T>, IReadOnlyList<T>
    {
    }
}
