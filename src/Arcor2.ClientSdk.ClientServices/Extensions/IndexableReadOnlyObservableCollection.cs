using Arcor2.ClientSdk.ClientServices.Managers;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Arcor2.ClientSdk.ClientServices.Extensions {

    /// <summary>
    /// Represents indexable <see cref="ReadOnlyObservableCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexableReadOnlyObservableCollection<T> : ReadOnlyObservableCollection<T> {
        private readonly Func<T, string> selector;

        /// <summary>
        /// Initializes a new instance of <see cref="IndexableReadOnlyObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="collection">The underlying observable collection.</param>
        /// <param name="selector">The selector used for index searches.</param>
        public IndexableReadOnlyObservableCollection(ObservableCollection<T> collection, Func<T, string> selector)
            : base(collection) {
            this.selector = selector;
        }

        public virtual T this[string id] => Items.FirstOrDefault(item => selector(item) == id);
    }

    /// <summary>
    /// Represents <see cref="IndexableReadOnlyObservableCollection{T}"/> constrained to <see cref="IArcor2Identity"/> types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Arcor2IndexableReadOnlyObservableCollection<T> : IndexableReadOnlyObservableCollection<T> where T : IArcor2Identity {
        public Arcor2IndexableReadOnlyObservableCollection(ObservableCollection<T> collection) : base(collection, manager => manager.Id) { }
        public override T this[string id] => Items.FirstOrDefault(item => item.Id == id)!;
    }
}
