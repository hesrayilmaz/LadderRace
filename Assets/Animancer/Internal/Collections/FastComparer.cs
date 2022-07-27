// Animancer // Copyright 2020 Kybernetik //

using System.Collections.Generic;

namespace Animancer
{
    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> which ignores overloaded equality operators so it is faster than
    /// <see cref="EqualityComparer{T}.Default"/> for types derived from <see cref="UnityEngine.Object"/>.
    /// </summary>
    public sealed class FastComparer : IEqualityComparer<object>
    {
        /************************************************************************************************************************/

        /// <summary>Singleton instance.</summary>
        public static readonly FastComparer Instance = new FastComparer();

        /// <summary>Calls <see cref="object.Equals(object, object)"/>.</summary>
        /// <remarks>
        /// We could use <see cref="object.ReferenceEquals"/> for slightly better performance, but that would not work
        /// for boxed value types (enums in particular).
        /// </remarks>
        bool IEqualityComparer<object>.Equals(object x, object y) { return Equals(x, y); }

        /// <summary>Calls <see cref="object.GetHashCode"/>.</summary>
        int IEqualityComparer<object>.GetHashCode(object obj) { return obj.GetHashCode(); }

        /************************************************************************************************************************/
    }
}

