// Synpl - a "structured editor" plugin for Gedit.
// Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>

// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Synpl.EditorAbstraction;

namespace Synpl.Core
{
    // TextWithChanges (TWC) rules:
    // 1. All changes are stored in 'old' coordinates.
    // 2. All calls receive parameters in 'new' (editor valid) coordinates.
    // 3. Deletes over inserts cancel themselves out.

    // Old coordinates are defined as coordinates in the _text (they
    // are affected by both deleted characters and newly added characters).


    // TODO: Must be able to save (MD5/SHA1 for the "actual" text, list of changes)
    // and restore by using a filename and the bytes for the hash and list of changes.

    // TODO: Optimizations: Add "insert range" and "delete range operations,
    // change the TextChange to allow multiple characters.
	public class TextWithChanges
	{
        #region Private Storage
        private CowList<TextChange> _changes;
        private string _text;
        private IAbstractEditor _editor;
        #endregion

        #region Constructor
		public TextWithChanges()
		{
            _changes = new CowList<TextChange>();
            _text = String.Empty;
		}

        public TextWithChanges(IAbstractEditor editor)
        {
            _changes = new CowList<TextChange>();
            _editor = editor;
            _text = _editor.GetText(0, _editor.Length);
        }
        #endregion

        #region Public Methods
        public void SetText(string text)
        {
            _changes = new CowList<TextChange>();
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
            InsertChar(ch, position, false);
        }
        
        public void InsertChar(char ch, int position, bool sendToEditor)
        {
            if (_editor != null && sendToEditor)
            {
                _editor.InsertText(position, "" + ch, true);
            }
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
            DeleteChar(position, false);
        }
        
        public void DeleteChar(int position, bool sendToEditor)
        {
            if (_editor != null && sendToEditor)
            {
                _editor.DeleteText(position, 1, true);
            }
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
            RemoveSliceWithChanges(start, end, false);
        }

        public void RemoveSliceWithChanges(int start, int end, bool sendToEditor)
        {
            if (_editor != null && sendToEditor)
            {
                _editor.DeleteText(start, end - start, true);
            }
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            CowList<TextChange> newChanges = new CowList<TextChange>();
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
            InsertSliceWithChanges(position, slice, false);
        }
        
        public void InsertSliceWithChanges(int position, TextWithChanges slice, bool sendToEditor)
        {
            if (_editor != null && sendToEditor)
            {
                CowList<CharWithPosition> sliceText = slice.GetCurrentSlice(0, slice.GetActualLength());
                StringBuilder sb = new StringBuilder();
                foreach (CharWithPosition cwp in sliceText)
                {
                    sb.Append(cwp.Char);
                }                
                _editor.InsertText(position, sb.ToString(), true);
            }
            int oldPosition = ConvertActualPositionToOld(position);
            CowList<TextChange> newChanges = new CowList<TextChange>();
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
                newChanges.Add(_changes[i].Moved(slice._text.Length));
            }
            _changes = newChanges;
            _text = _text.Substring(0, oldPosition) + slice._text + _text.Substring(oldPosition);
        }

        public CowList<CharWithPosition> GetCurrentSlice(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            CowList<TextChange> deletes = new CowList<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsDeletion && change.IsBetween(oldStart, oldEnd)) 
                {
                    deletes.Add(change);
                }
            }
            CowList<CharWithPosition> result = new CowList<CharWithPosition>();
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

        public CowList<CharWithPosition> GetOldSlice(int start, int end)
        {
            int oldStart = ConvertActualPositionToOld(start);
            int oldEnd = ConvertActualPositionToOld(end, true);
            CowList<TextChange> relevantChanges = new CowList<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsBetween(oldStart, oldEnd)) {
                    relevantChanges.Add(change);
                }
            }
            CowList<CharWithPosition> result = new CowList<CharWithPosition>();
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

            CowList<TextChange> changesBefore = new CowList<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.Position < oldStart) {
                    changesBefore.Add(change);
                }
            }

            CowList<TextChange> deletesBetween = new CowList<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.IsDeletion && change.IsBetween(oldStart, oldEnd))
                {
                    deletesBetween.Add(change);
                }
            }

            int offset = - deletesBetween.Count;
            CowList<TextChange> changesAfter = new CowList<TextChange>();
            foreach (TextChange change in _changes)
            {
                if (change.Position >= oldEnd)
                {
                    changesAfter.Add(change.Moved(offset));
                }
            }
            _changes = changesBefore;
            _changes.AddRange(changesAfter);

            int runningOffset = 0;
            foreach (TextChange delete in deletesBetween)
            {
                int pos = delete.Position + runningOffset;
                _text = _text.Remove(pos, 1);
                runningOffset -= 1;
            }
        }
        #endregion
	}
}
