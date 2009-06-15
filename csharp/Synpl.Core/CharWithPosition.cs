
using System;

namespace Synpl.Core
{    
    public class CharWithPosition
    {
        #region Private Storage
        private int _position;
        private char _char;
        #endregion

        #region Properties
        public char Char {
            get {
                return _char;
            }
        }

        public int Position {
            get {
                return _position;
            }
        }
        #endregion

        #region Constructor
        public CharWithPosition(int position, char ch)
        {
            _position = position;
            _char = ch;
        }
        #endregion

        #region Public Methods
        public override string ToString ()
        {
            return string.Format("new CharWithPosition({1}, '{0}')", Char, Position);
        }

        public override bool Equals (object obj)
        {
            CharWithPosition other = obj as CharWithPosition;
            if (other == null)
            {
                return false;
            }
            return _position == other._position && _char == other._char;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #endregion
    }
}
