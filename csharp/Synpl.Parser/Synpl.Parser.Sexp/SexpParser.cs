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
using Synpl.Core;
using System.Collections.Generic;
using System.Text;

namespace Synpl.Parser.Sexp
{
    public class SexpParser : Synpl.Core.Parser, IIdGenerator
    {
        public enum TokenTypes {
            OpenParen = 1,
            CloseParen,
            Quote,
            Atom
        };
        
        #region Private fields
        #endregion

        #region Constructor
        public SexpParser() : base(Parse, Tokenize, new DefaultIdGenerator())
        {
        }
        #endregion

        #region Parser Implementation
        public static void Parse(List<Token> tokens, 
                                      TextWithChanges text,
                                      out ParseTree parseTree,
                                      out List<Token> remainingTokens)
        {
            parseTree = null;
            remainingTokens = null;
        }

        public static List<Token> Tokenize(List<CharWithPosition> text)
        {
            List<Token> result = new List<Token>();
            for (int pos = 0; pos < text.Count;)
            {
                CharWithPosition ch = text[pos];
                if (ch.Char =='(')
                {
                    result.Add(new Token((int)TokenTypes.OpenParen, "(", ch.Position, ch.Position + 1));
                    pos ++;
                }
                else if (ch.Char == ')')
                {
                    result.Add(new Token((int)TokenTypes.CloseParen, ")", ch.Position, ch.Position + 1));
                    pos++;
                }
                else if (Char.IsWhiteSpace(ch.Char))
                {
                    while (pos < text.Count && Char.IsWhiteSpace(text[pos].Char))
                    {
                        pos ++;
                    }
                }
                else if (ch.Char == '\'')
                {
                    result.Add(new Token((int)TokenTypes.Quote, "'", ch.Position, ch.Position + 1));
                    pos ++;
                }
                else if (ch.Char == ';')
                {
                    while (pos < text.Count && text[pos].Char != '\n')
                    {
                        pos++;
                    }
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    int startPos = ch.Position;
                    int endPos = startPos;
                    while (pos < text.Count 
                           && !Char.IsWhiteSpace(text[pos].Char)
                           && text[pos].Char != '('
                           && text[pos].Char != ')'
                           && text[pos].Char != '\'')
                    {
                        sb.Append(text[pos].Char);
                        pos ++;
                        endPos ++;
                    }
                    result.Add(new Token((int)TokenTypes.Atom, sb.ToString(), startPos, endPos));
                }
            }
            return result;
        }
        #endregion
    }
}
