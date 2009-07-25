// Synpl - a "structured editor".
// Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
// 

using System;

namespace Synpl.Core
{
    // TODO: Add unit tests.
    public class TwcBuilder
    {        
        #region Private Storage
        TextWithChanges _text;
        #endregion

        #region Constructor
        public TwcBuilder()
        {
            _text = new TextWithChanges();
        }
        #endregion

        #region Public Methods
        public TextWithChanges ToTwc()
        {
            return _text;
        }

        public void AddTwc(TextWithChanges twc)
        {
            _text.InsertSliceWithChanges(_text.GetActualLength(), twc);                                         
        }

        public void AddText(string text)
        {
            TextWithChanges twc = new TextWithChanges();
            twc.SetText(text);
            AddTwc(twc);
        }

        public void DeleteLastCharAsChange()
        {
            _text.DeleteChar(_text.GetActualLength() - 1);
        }

        public void AppendCharAsChange(char ch)
        {
            _text.InsertChar(ch, _text.GetActualLength());
        }
        #endregion
    }
}
