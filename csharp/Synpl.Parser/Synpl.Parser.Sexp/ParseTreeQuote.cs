// Synpl - a "structured editor".
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
using Synpl.Core;
using System.Collections.Generic;

namespace Synpl.Parser.Sexp
{
    public class ParseTreeQuote : ParseTree
    {
        #region Constructor
        public ParseTreeQuote(int startPosition, 
                              int endPosition, 
                              ParseTree quotedTree,
                              Synpl.Core.Parser parser,
                              ParseTree parent,
                              TextWithChanges text)
            : base(startPosition,
                   endPosition,
                   MakeList(quotedTree),
                   parser,
                   parent,
                   text,
                   "' (quote)")
        {
        }
        #endregion

        #region Private Helper Methods
        private static CowList<ParseTree> MakeList(ParseTree item)
        {
            CowList<ParseTree> result = new CowList<ParseTree>();
            result.Add(item);
            return result;
        }
        #endregion

        #region Pretty Printing
        public override string ToStringAsPrettyPrint(int indentLevel, int maxColumn)
        {
            return String.Format("'{0}", SubTrees[0].ToStringAsPrettyPrint(indentLevel + 1, maxColumn));
        }
        #endregion
    }
}
