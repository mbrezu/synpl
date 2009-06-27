
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Synpl.Core
{
    public class CowList<T> : IList<T>, IEnumerable<T>
    {
        #region Private Storage
        private SharedList<T> _storage;
        private int _offset;
        private int _length;
        #endregion

        #region Inner Classes
        private class CowListEnumerator<U> : IEnumerator<U>, IEnumerator
        {
            #region Private Storage
            private List<U> _storage;
            private int _offset;
            private int _length;
            private int _currentPosition;
            #endregion

            #region Constructor
            public CowListEnumerator(CowList<U> cowList)
            {
                _storage = cowList._storage.Storage;
                _offset = cowList._offset;
                _length = cowList._length;
                _currentPosition = cowList._offset - 1;
            }
            #endregion

            #region IEnumerator Implementation
            public U Current
            {
                get
                {
                    return _storage[_currentPosition];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_currentPosition < _offset)
                    {
                        throw new InvalidOperationException("Call MoveNext() first.");
                    }
                    return _storage[_currentPosition];
                }
            }

            public bool MoveNext()
            {
                if (_currentPosition < _offset + _length)
                {
                    _currentPosition ++;
                }
                return _currentPosition < _offset + _length;
            }

            public void Reset()
            {
                _currentPosition = _offset - 1;
            }

            public void Dispose()
            {
            }
            #endregion
        }

        private class SharedList<U>
        {
            #region Private Storage
            private List<U> _storage;
            private int _users;
            #endregion

            #region Properties
            public List<U> Storage
            {
                get
                {
                    return _storage;
                }
            }
            #endregion

            #region Constructor
            public SharedList()
            {
                _storage = new List<U>();
                _users = 1;
            }

            public SharedList(List<U> storage)
            {
                _storage = storage;
                _users = 1;
            }            
            #endregion

            #region Properties
            public bool IsShared
            {
                get
                {
                    return _users > 1;
                }
            }
            #endregion

            #region Public Methods
            public void IncreaseUsers()
            {
                _users ++;
            }

            public SharedList<U> Detach(int index, int length)
            {
                SharedList<U> result = new SharedList<U>();
                result._storage = _storage.GetRange(index, length);
                DecreaseUsers();
                return result;
            }

            public SharedList<U> Detach()
            {
                return Detach(0, _storage.Count);
            }

            public void DecreaseUsers()
            {
                _users --;
            }
            #endregion
        }

        #endregion

        #region Private Helper Methods
        private void GetOwnCopy()
        {
            if (_storage.IsShared)
            {
                _storage = _storage.Detach(_offset, _length);
                _offset = 0;
                _length = _storage.Storage.Count;
            }
        }
        #endregion

        #region IList Implementation
        public void Add(T item)
        {
            GetOwnCopy();
            if (_offset + _length < _storage.Storage.Count)
            {
                _storage.Storage[_offset + _length] = item;
            }
            else
            {
                _storage.Storage.Add(item);
            }
            _length ++;
        }

        public void Clear()
        {
            GetOwnCopy();
            _storage.Storage.Clear();
            _offset = 0;
            _length = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int index)
        {
            for (int i = _offset; i < _offset + _length; i++)
            {
                array[index] = _storage.Storage[i];
                index ++;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new CowListEnumerator<T>(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new CowListEnumerator<T>(this);
        }

        public int IndexOf(T item)
        {
            for (int i = _offset; i < _offset + _length; i++)
            {
                if (_storage.Storage[i] != null && _storage.Storage[i].Equals(item))
                {
                    return i - _offset;
                }
            }
            return -1;            
        }

        public void Insert(int index, T item)
        {
            GetOwnCopy();
            if (index > _length)
            {
                Add(item);
            }
            else
            {
                _storage.Storage.Insert(_offset + index, item);
                _length ++;
            }
        }

        public bool Remove(T item)
        {
            GetOwnCopy();
            int index = IndexOf(item);
            if (index != -1)
            {
                _storage.Storage.RemoveAt(_offset + index);
                _length --;
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            GetOwnCopy();
            if (index >= 0 && index < _length)
            {
                _storage.Storage.RemoveAt(_offset + index);
                _length --;
            }
        }

        public int Count
        {
            get
            {
                return _length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                {
                    throw new ArgumentOutOfRangeException("Invalid index.");
                }
                return _storage.Storage[_offset + index];
            }
            set
            {
                if (index < 0 || index >= _length)
                {
                    throw new ArgumentOutOfRangeException("Invalid index.");
                }
                GetOwnCopy();
                _storage.Storage[_offset + index] = value;
            }
        }
        #endregion

        #region Properties
        public T Last
        {
            get
            {
                return _storage.Storage[_offset + _length - 1];
            }
        }
        
        public T Head
        {
            get
            {
                return _storage.Storage[_offset];
            }            
        }

        public CowList<T> Tail
        {
            get
            {
                CowList<T> result = new CowList<T>();
                result._storage = _storage;
                result._offset = _offset + 1;
                result._length = _length - 1;
                _storage.IncreaseUsers();
                return result;
            }
        }
        #endregion

        #region Public methods
        public void Sort()
        {
            GetOwnCopy();
            _storage.Storage.Sort();
        }

        public void AddRange(IList<T> others)
        {
            GetOwnCopy();
            foreach (T item in others)
            {
                Add(item);
            }
        }

        public override bool Equals (object obj)
        {
            CowList<T> other = obj as CowList<T>;
            if (other == null)
            {
                return false;
            }
            if (Count != other.Count)
            {
                return false;
            }
            for (int i = 0; i < Count; i++)
            {
                if (this[i] == null && other[i] == null)
                {
                    continue;
                }
                if (this[i] != null && !this[i].Equals(other[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (T item in this)
            {
                sb.Append(item.ToString());
                sb.Append(" ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public override int GetHashCode ()
        {
            return ToString().GetHashCode();
        }
        
        #endregion
        
        #region Constructor
        public CowList()
        {
            _storage = new SharedList<T>();
            _offset = 0;
            _length = 0;
        }

        public CowList(List<T> storage)
        {
            _storage = new SharedList<T>(storage);
            _offset = 0;
            _length = storage.Count;
        }

        public CowList(params T[] args) : this()
        {
            foreach (T item in args)
            {
                Add(item);
            }
        }
        #endregion

        #region Destructor
        ~CowList()
        {
            _storage.DecreaseUsers();
        }
        #endregion
    }
}
