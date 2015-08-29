/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicLib
{
    public class FastAccessList<K, T> : IList<T>, INotifyCollectionChanged
    {
        List<T> list = new List<T>();
        Dictionary<K, T> dict = new Dictionary<K, T>();
        Func<T, K> keyDerivingFunc;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public FastAccessList(Func<T, K> keyDerivingFunc)
        {
            this.keyDerivingFunc = keyDerivingFunc;
        }

        public int Count
        {
            get
            {
                return ((IList<T>)list).Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IList<T>)list).IsReadOnly;
            }
        }

        public T this[int index]
        {
            get
            {
                return ((IList<T>)list)[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)list).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)list).Insert(index, item);
            dict.Add(keyDerivingFunc(item), item);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void RemoveAt(int index)
        {
            T item = list[index];
            dict.Remove(keyDerivingFunc(item));
            ((IList<T>)list).RemoveAt(index);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void Add(T item)
        {
            ((IList<T>)list).Add(item);
            dict.Add(keyDerivingFunc(item), item);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, list.Count - 1));
        }

        public void Clear()
        {
            ((IList<T>)list).Clear();
            dict.Clear();
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return dict.ContainsKey(keyDerivingFunc(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((IList<T>)list).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            dict.Remove(keyDerivingFunc(item));
            bool result = ((IList<T>)list).Remove(item);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }

        public T FastGet(K key)
        {
            return dict[key];
        }

        public bool FastTryGet(K key, out T result)
        {
            return dict.TryGetValue(key, out result);
        }

        public bool ContainsKey(K key)
        {
            return dict.ContainsKey(key);
        }
    }
}
