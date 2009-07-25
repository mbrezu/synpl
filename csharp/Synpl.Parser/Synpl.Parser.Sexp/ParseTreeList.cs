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
    public class ParseTreeList : ParseTree
    {
        #region Constructor
        public ParseTreeList(int startPosition, 
                              int endPosition, 
                              CowList<ParseTree> members,
                              Synpl.Core.Parser parser,
                              ParseTree parent,
                              TextWithChanges text)
            : base(startPosition,
                   endPosition,
                   members,
                   parser,
                   parent,
                   text,
                   "() (list)")
        {
        }
        #endregion

        #region Pretty Printing
        public override TextWithChanges ToTwcSliceAsPrettyPrint(int indentLevel, 
                                                                int maxColumn)
        {
            if (SubTrees.Count == 0 || HasUnparsedChanges())
            {
                return base.ToTwcSliceAsPrettyPrint(indentLevel, maxColumn);
            }
            else
            {
                TwcBuilder result = new TwcBuilder();
                result.AddText("(");
                foreach (ParseTree tree in SubTrees)
                {
                    result.AddTwc(tree.ToTwcSliceAsPrettyPrint(indentLevel, 100000));
                    if (tree != SubTrees.Last)
                    {
                        result.AddText(" ");
                    }
                }
                result.AddText(")");
                if (indentLevel + result.ToTwc().GetActualLength() <= maxColumn)
                {
                    return result.ToTwc();
                }
                // Second attempt, this time one child per line.
                TwcBuilder result2 = new TwcBuilder();
                result2.AddText("(");
                string separator = "\n" + new String(' ', indentLevel + 1);
                foreach (ParseTree tree in SubTrees)
                {
                    result2.AddTwc(tree.ToTwcSliceAsPrettyPrint(indentLevel + 1, maxColumn));
                    if (tree != SubTrees.Last)
                    {
                        result2.AddText(separator);
                    }
                }
                result2.AddText(")");
                return result2.ToTwc();
            }
        }
        #endregion

    }
}
