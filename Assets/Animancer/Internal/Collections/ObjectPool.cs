// Animancer // Copyright 2020 Kybernetik //

//#define ANIMANCER_LOG_OBJECT_POOLING

using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>Convenience methods for accessing <see cref="ObjectPool{T}"/>.</summary>
    public static class ObjectPool
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Acquire"/> to get a spare item if there are any, or create a new one.
        /// </summary>
        public static T Acquire<T>() where T : class, new()
        {
            return ObjectPool<T>.Acquire();
        }

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Acquire"/> to get a spare item if there are any, or create a new one.
        /// </summary>
        public static void Acquire<T>(out T item) where T : class, new()
        {
            item = ObjectPool<T>.Acquire();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Release"/> to add the `item` to the list of spares so it can be reused.
        /// </summary>
        public static void Release<T>(T item) where T : class, new()
        {
            ObjectPool<T>.Release(item);
        }

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Release"/> to add the `item` to the list of spares so it can be reused.
        /// </summary>
        public static void Release<T>(ref T item) where T : class, new()
        {
            if (item != null)
            {
                ObjectPool<T>.Release(item);
                item = null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Acquire"/> to get a spare <see cref="List{T}"/> if
        /// there are any or create a new one.
        /// </summary>
        public static List<T> AcquireList<T>()
        {
            var list = ObjectPool<List<T>>.Acquire();
            Debug.Assert(list.Count == 0,
                "A pooled collection is not empty. Collections must not be modified after being released to the pool.");
            return list;
        }

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Release"/> to clear the `list` and mark it as a spare
        /// so it can be later returned by <see cref="AcquireList"/>.
        /// </summary>
        public static void Release<T>(List<T> list)
        {
            list.Clear();
            ObjectPool<List<T>>.Release(list);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Acquire"/> to get a spare <see cref="HashSet{T}"/> if
        /// there are any or create a new one.
        /// </summary>
        public static HashSet<T> AcquireSet<T>()
        {
            var set = ObjectPool<HashSet<T>>.Acquire();
            Debug.Assert(set.Count == 0,
                "A pooled collection is not empty. Collections must not be modified after being released to the pool.");
            return set;
        }

        /// <summary>
        /// Calls <see cref="ObjectPool{T}.Release"/> to clear the `set` and mark it as a spare
        /// so it can be later returned by <see cref="AcquireSet"/>.
        /// </summary>
        public static void Release<T>(HashSet<T> set)
        {
            set.Clear();
            ObjectPool<HashSet<T>>.Release(set);
        }

        /************************************************************************************************************************/
    }

    /// <summary>A simple object pooling system.</summary>
    public static class ObjectPool<T> where T : class, new()
    {
        /************************************************************************************************************************/

        private static readonly List<T>
            Items = new List<T>();

        /************************************************************************************************************************/

        /// <summary>The number of spare items currently in the pool.</summary>
        public static int Count
        {
            get { return Items.Count; }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="List{T}.Capacity"/> of the internal list of spare items.</summary>
        public static int Capacity
        {
            get { return Items.Capacity; }
            set
            {
                if (Items.Count > value)
                    Items.RemoveRange(value, Items.Count - value);
                Items.Capacity = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns a spare item if there are any, or creates a new one.</summary>
        public static T Acquire()
        {
            var count = Items.Count;
            if (count == 0)
            {
                return new T();
            }
            else
            {
                count--;
                var item = Items[count];
                Items.RemoveAt(count);

                return item;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `item` to the list of spares so it can be reused.</summary>
        public static void Release(T item)
        {
            Items.Add(item);

        }

        /************************************************************************************************************************/
    }
}

