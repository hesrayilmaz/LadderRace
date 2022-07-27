// Animancer // Copyright 2020 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// An object with a <see cref="Animancer.Key"/> so it can be used in a <see cref="Key.KeyedList{T}"/>.
    /// </summary>
    public interface IKeyHolder
    {
        /// <summary>The <see cref="Animancer.Key"/> which stores the list index of this object.</summary>
        Key Key { get; }
    }

    /// <summary>
    /// Stores the index of an object in a <see cref="KeyedList{T}"/> to allow it to be efficiently removed.
    /// </summary>
    public class Key : IKeyHolder
    {
        /************************************************************************************************************************/

        private int _Index = -1;

        /// <summary>Returns location of this object in the list (or -1 if it is not currently in a keyed list).</summary>
        public static int IndexOf(Key key) { return key._Index; }

        /// <summary>Indicates whether the specified object is currently in a keyed list.</summary>
        public static bool IsInList(Key key) { return key._Index != -1; }

        /************************************************************************************************************************/

        /// <summary>A <see cref="Key"/> is its own <see cref="Key"/>.</summary>
        Key IKeyHolder.Key { get { return this; } }

        /************************************************************************************************************************/

        /// <summary>
        /// A <see cref="List{T}"/> which can remove items without needing to search through the entire collection.
        /// Does not allow nulls to be added.
        /// </summary>
        ///
        /// <example>
        /// To use an object in a Keyed List, it must either inherit from <see cref="Key"/> or implement
        /// <see cref="IKeyHolder"/> like so:
        /// <code>
        /// class MyClass : IKeyHolder
        /// {
        ///     private readonly Key Key = new Key();
        ///     Key IKeyHolder.Key { get { return Key; } }
        /// }
        /// </code>
        /// Note that the <c>Key</c> field can be made <c>public</c> if desired.
        /// </example>
        ///
        /// <remarks>
        /// This class is nested inside <see cref="Key"/> so it can modify the private <see cref="_Index"/> without
        /// exposing that capability to anything else.
        /// </remarks>
        public sealed class KeyedList<T> : IList<T> where T : class, IKeyHolder
        {
            /************************************************************************************************************************/

            private const string
                SingleUse = "Each item can only be used in one Keyed List at a time.",
                NotFound = "The specified item does not exist in this list.";

            /************************************************************************************************************************/

            private readonly List<T> Items;

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="KeyedList{T}"/> using the default <see cref="List{T}"/> constructor.</summary>
            public KeyedList()
            {
                Items = new List<T>();
            }

            /// <summary>Creates a new <see cref="KeyedList{T}"/> with the specified initial `capacity`.</summary>
            public KeyedList(int capacity)
            {
                Items = new List<T>(capacity);
            }

            // No copy constructor because the keys will not work if they are used in multiple lists at once.

            /************************************************************************************************************************/

            /// <summary>The number of items currently in the list.</summary>
            public int Count { get { return Items.Count; } }

            /// <summary>The number of items that this list can contain before resizing is required.</summary>
            public int Capacity { get { return Items.Capacity; } set { Items.Capacity = value; } }

            /************************************************************************************************************************/

            /// <summary>Gets or sets the item at the specified `index`.</summary>
            /// <exception cref="ArgumentException">Thrown by the setter if the `value` was already in a keyed list.</exception>
            public T this[int index]
            {
                get { return Items[index]; }
                set
                {
                    var key = value.Key;
                    if (key._Index != -1)
                        throw new ArgumentException(SingleUse);

                    var item = Items[index];
                    item.Key._Index = -1;

                    key._Index = index;
                    Items[index] = value;
                }
            }

            /************************************************************************************************************************/

            /// <summary>Adds the `item` to the end of this list.</summary>
            /// <exception cref="ArgumentException">Thrown if the `item` was already in a keyed list.</exception>
            public void Add(T item)
            {
                var key = item.Key;
                if (key._Index != -1)
                    throw new ArgumentException(SingleUse);

                key._Index = Items.Count;
                Items.Add(item);
            }

            /// <summary>Adds the `item` to the end of this list if it wasn't already in one.</summary>
            public void AddNew(T item)
            {
                if (!IsInList(item.Key))
                    Add(item);
            }

            /************************************************************************************************************************/

            /// <summary>Removes the item at the specified `index`.</summary>
            public void RemoveAt(int index)
            {
                Items[index].Key._Index = -1;
                Items.RemoveAt(index);
            }

            /// <summary>
            /// Removes the item at the specified `index` by swapping the last item in this list into its place.
            /// <para></para>
            /// This does not maintain the order of items, but is more efficient than <see cref="RemoveAt"/> because
            /// it avoids the need to move every item after the removed one down one place.
            /// </summary>
            public void RemoveAtSwap(int index)
            {
                Items[index].Key._Index = -1;

                var lastIndex = Items.Count - 1;
                if (lastIndex > index)
                {
                    var lastItem = Items[lastIndex];
                    lastItem.Key._Index = index;
                    Items[index] = lastItem;
                }

                Items.RemoveAt(lastIndex);
            }

            /************************************************************************************************************************/

            /// <summary>Removes the `item` from this list.</summary>
            public bool Remove(T item)
            {
                var key = item.Key;
                var index = key._Index;
                if (index < 0)
                    return false;

                Debug.Assert(Items[index] == item, NotFound);

                key._Index = -1;
                Items.RemoveAt(index);
                return true;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Removes the `item` by swapping the last item in this list into its place.
            /// <para></para>
            /// This does not maintain the order of items, but is more efficient than <see cref="Remove"/> because
            /// it avoids the need to move every item after the removed one down one place.
            /// </summary>
            public bool RemoveSwap(T item)
            {
                var key = item.Key;
                var index = key._Index;
                if (index < 0)
                    return false;

                Debug.Assert(Items[index] == item, NotFound);

                key._Index = -1;

                var lastIndex = Items.Count - 1;
                if (lastIndex > index)
                {
                    var lastItem = Items[lastIndex];
                    lastItem.Key._Index = index;
                    Items[index] = lastItem;
                }

                Items.RemoveAt(lastIndex);
                return true;
            }

            /************************************************************************************************************************/

            /// <summary>Removes all items from this list.</summary>
            public void Clear()
            {
                for (int i = Items.Count - 1; i >= 0; i--)
                {
                    Items[i].Key._Index = -1;
                }

                Items.Clear();
            }

            /************************************************************************************************************************/

            /// <summary>Indicates whether the `item` is currently in this list.</summary>
            public bool Contains(T item)
            {
                if (item == null)
                    return false;

                var index = item.Key._Index;
                return
                    index >= 0 &&
                    index < Items.Count &&
                    Items[index] == item;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the index of the `item` in this list or -1 if it is not in this list.</summary>
            public int IndexOf(T item)
            {
                if (item == null)
                    return -1;

                var index = item.Key._Index;
                if (index >= 0 &&
                    index < Items.Count &&
                    Items[index] == item)
                    return index;
                else
                    return -1;
            }

            /************************************************************************************************************************/

            /// <summary>Adds the `item` to this list at the specified `index`.</summary>
            public void Insert(int index, T item)
            {
                for (int i = index; i < Items.Count; i++)
                    Items[i].Key._Index++;

                item.Key._Index = index;
                Items.Insert(index, item);
            }

            /************************************************************************************************************************/

            /// <summary>Copies all the items from this list into the `array`, starting at the specified `arrayIndex`.</summary>
            public void CopyTo(T[] array, int arrayIndex)
            {
                Items.CopyTo(array, arrayIndex);
            }

            /// <summary>Returns false.</summary>
            bool ICollection<T>.IsReadOnly { get { return false; } }

            /// <summary>Returns an enumerator that iterates through this list.</summary>
            public IEnumerator<T> GetEnumerator()
            {
                return Items.GetEnumerator();
            }

            /// <summary>Returns an enumerator that iterates through this list.</summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return Items.GetEnumerator();
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

