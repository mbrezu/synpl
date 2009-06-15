
using System;

namespace Synpl.Core
{
    public class TextChange : IComparable
    {
        #region Private Storage
        private int _position;
        private bool _isDeletion;
        #endregion

        #region Properties
        public bool IsDeletion {
            get {
                return _isDeletion;
            }
        }

        public int Position {
            get {
                return _position;
            }
        }
        #endregion
        
        #region Constructor
        public TextChange(int position, bool isDeletion)
        {
            _position = position;
            _isDeletion = isDeletion;
        }
        #endregion

        #region Public Methods
        public TextChange Moved(int offset)
        {
            return new TextChange(_position + offset, _isDeletion);
        }

        public bool IsBetween(int start, int end)
        {
            return _position >= start && _position < end;
        }
        #endregion

        #region IComparable Implementation
        public int CompareTo(object other)
        {
            TextChange tcOther = other as TextChange;
            if (tcOther == null) {
                return 0;
            }
            return _position - tcOther.Position;
        }
        #endregion
    }
}
