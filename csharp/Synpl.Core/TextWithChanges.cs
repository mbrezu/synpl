
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Synpl.Core
{
    // TextWithChanges (TWC) rules:
    // 1. All changes are stored in 'old' coordinates.
    // 2. All calls receive parameters in 'new' (editor valid) coordinates.
    // 3. Deletes over inserts cancel themselves out.

    // Old coordinates are defined as coordinates in the _text (they
    // are affected by both deleted characters and newly added characters).

	public class TextWithChanges
	{
        #region Private Storage
        private List<TextChange> _changes;
        private string _text;
        #endregion

        #region Constructor
		public TextWithChanges()
		{
            _changes = new List<TextChange>();
            _text = String.Empty;
		}
        #endregion

        #region Public Methods
        public void SetText(string text)
        {
            _changes = new List<TextChange>();
            _text = text;
        }

        public ReadOnlyCollection<TextChange> GetChanges()
        {
            return new ReadOnlyCollection<TextChange>(_changes);
        }
        
        public int ConvertActualPositionToOld(int actualPosition, bool findEnd)
        {
            int result = actualPosition;
            foreach (TextChange tc in _changes)
            {
                if (tc.Position > result)
                {
                    break;
                }
                else if (tc.IsDeletion)
                {
                    if (findEnd || tc.Position < result) {
                        result ++;
                    }
                }
            }
            return result;
        }
        
        public int ConvertActualPositionToOld(int actualPosition)
        {
            return ConvertActualPositionToOld(actualPosition, false);
        }

        public void InsertChar(char ch, int position)
        {
            int oldPosition = ConvertActualPositionToOld(position);
            _text = _text.Substring(0, oldPosition) + ch + _text.Substring(oldPosition);
            // Move all marks one position forward.
            for (int i = 0; i < _changes.Count; i++)
            {
                if (_changes[i].Position >= oldPosition)
                {
                    _changes[i] = _changes[i].Moved(1);
                }
            }
            _changes.Add(new TextChange(oldPosition, false));
            _changes.Sort();
        }

        public void DeleteChar(int position)
        {
            int oldPosition = ConvertActualPositionToOld(position, true);
            // Check if we're actually deleting an insertion; if so, they
            // will cancel themselves out and all changes afterwards will
            // be pulled back one character.
            bool insertionCanceled = false;

            int i = 0;
            for (i = 0; i < _changes.Count; i++)
            {
                if (_changes[i].Position == oldPosition && !_changes[i].IsDeletion)
                {
                    _text = _text.Remove(oldPosition, 1);
                    insertionCanceled = true;
                    _changes.RemoveAt(i);
                    break;
                }
            }
            if (insertionCanceled)
            {
                for (; i < _changes.Count; i++) 
                {
                    _changes[i] = _changes[i].Moved(-1);
                }
            }
            else
            {
                _changes.Add(new TextChange(oldPosition, true));
                _changes.Sort();
            }
        }

        public string TestRender()
        {
            StringBuilder sb = new StringBuilder();
            int k = 0;
            for (int i = 0; i < _text.Length; i++)
            {
                if (k < _changes.Count && _changes[k].Position == i)
                {
                    if (_changes[k].IsDeletion) 
                    {
                        sb.Append("D");
                    }
                    else
                    {
                        sb.Append("A");
                    }
                    k++;
                }
                else
                {
                    sb.Append("_");
                }
                sb.Append(_text[i]);
            }
            return sb.ToString();
        }

        public TextWithChanges GetSliceWithChanges(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            TextWithChanges result = new TextWithChanges();
            foreach (TextChange change in _changes)
            {
                if (change.IsBetween(oldStart, oldEnd)) {
                    result._changes.Add(change.Moved(-oldStart));
                }
            }
            result._text = _text.Substring(oldStart, oldEnd - oldStart);
            return result;
        }

        public void RemoveSliceWithChanges(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            List<TextChange> newChanges = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.Position < oldStart) {
                    newChanges.Add(change);
                }
            }
            int offset = -(oldEnd - oldStart);
            foreach (TextChange change in _changes)
            {
                if (change.Position >= oldEnd) {
                    newChanges.Add(change.Moved(offset));
                }
            }
            _changes = newChanges;
            _text = _text.Remove(oldStart, oldEnd - oldStart);
        }

        public int GetActualLength()
        {
            int result = _text.Length;
            foreach (TextChange change in _changes)
            {
                if (change.IsDeletion)
                {
                    result --;
                }
            }
            return result;
        }

        public int GetOldLength()
        {
            return _text.Length;
        }

        public void InsertSliceWithChanges(int position, TextWithChanges slice)
        {
            int oldPosition = ConvertActualPositionToOld(position);
            List<TextChange> newChanges = new List<TextChange>();
            // Select changes before the insert position.
            int i;
            for (i = 0; i < _changes.Count; i++)
            {
                if (_changes[i].Position < oldPosition)
                {
                    newChanges.Add(_changes[i]);
                }
                else
                {
                    break;
                }
            }
            // Add changes from the inserted slice.
            foreach (TextChange change in slice._changes)
            {
                newChanges.Add(change.Moved(oldPosition));
            }
            // Add changes that were after the insert position.
            for (; i < _changes.Count; i++)
            {
                newChanges.Add(_changes[i]);
            }
            _changes = newChanges;
            _text = _text.Substring(0, oldPosition) + slice._text + _text.Substring(oldPosition);
        }

        public List<CharWithPosition> GetCurrentSlice(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            List<TextChange> deletes = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsDeletion && change.IsBetween(oldStart, oldEnd)) 
                {
                    deletes.Add(change);
                }
            }
            List<CharWithPosition> result = new List<CharWithPosition>();
            int k = 0;
            int runningIndex = start;
            for (int i = oldStart; i < oldEnd && i < _text.Length; i++)
            {
                if (k < deletes.Count)
                {
                    if (deletes[k].Position == i)
                    {
                        k ++;
                        continue;
                    }
                }
                result.Add(new CharWithPosition(runningIndex, _text[i]));
                runningIndex++;
            }
            return result;
        }

        public List<CharWithPosition> GetOldSlice(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            List<TextChange> relevantChanges = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsBetween(oldStart, oldEnd)) {
                    relevantChanges.Add(change);
                }
            }
            List<CharWithPosition> result = new List<CharWithPosition>();
            int k = 0;
            int runningIndex = start;
            for (int i = oldStart; i < oldEnd && i < _text.Length; i++)
            {
                bool isDeletion = false;
                if (k < relevantChanges.Count) 
                {
                    if (relevantChanges[k].Position == i) {
                        if (relevantChanges[k].IsDeletion)
                        {
                            isDeletion = true;
                            k++;
                        }
                        else
                        {
                            runningIndex += 1;
                            k++;
                            continue;
                        }
                    }
                }
                result.Add(new CharWithPosition(runningIndex, _text[i]));
                if (!isDeletion)
                {
                    runningIndex += 1;
                }
            }
            return result;
        }

        public void ValidateSlice(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);

            List<TextChange> changesBefore = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.Position < oldStart) {
                    changesBefore.Add(change);
                }
            }

            List<TextChange> deletesBetween = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsDeletion && change.IsBetween(oldStart, oldEnd))
                {
                    deletesBetween.Add(change);
                }
            }

            int offset = - deletesBetween.Count;
            List<TextChange> changesAfter = new List<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.Position >= oldEnd)
                {
                    deletesBetween.Add(change.Moved(offset));
                }
            }          
            _changes = changesBefore;
            _changes.AddRange(changesAfter);

            int runningOffset = 0;
            foreach (TextChange delete in deletesBetween)
            {
                int pos = delete.Position + runningOffset;
                _text = _text.Substring(0, pos) + _text.Substring(pos + 1);
                runningOffset -= 1;
            }
        }
        #endregion
	}
}
