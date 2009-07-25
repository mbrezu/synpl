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
using System.Text;

namespace Synpl.Parser.Sexp
{
    public class SexpParser : Synpl.Core.Parser
    {
        public enum TokenTypes {
            OpenParen = 1,
            CloseParen,
            Quote,
            Atom
        };

        public enum ParseType {
            Global,
            Sexp
        }

        #region Private Static Storage
        private static SexpParser _globalInstance = null;
        private static SexpParser _sexpInstance = null;
        #endregion
        
        #region Public Methods
        public static SexpParser GetInstance()
        {
            return GetInstance(ParseType.Sexp);
        }
        
        public static SexpParser GetInstance(ParseType parseType)
        {
            switch (parseType)
            {
            case ParseType.Global:
                if (_globalInstance == null)
                {
                    _globalInstance = new SexpParser();
                    _globalInstance.ParserFunc = ParseGlobal;
                }
                return _globalInstance;
            case ParseType.Sexp:
                if (_sexpInstance == null)
                {
                    _sexpInstance = new SexpParser();
                    _sexpInstance.ParserFunc = ParseLocal;
                }
                return _sexpInstance;
            default:
                throw new ArgumentException("Unknown Sexp Parser type.");
            }                    
        }

        #endregion
        
        #region Constructor
        private SexpParser() : base(ParseLocal, Tokenize)
        {            
        }
        #endregion

        #region Parser Implementation
        public static void ParseGlobal(CowList<Token> tokens, 
                                      TextWithChanges text,
                                      out ParseTree parseTree,
                                      out CowList<Token> remainingTokens)
        {
            CowList<ParseTree> trees = new CowList<ParseTree>();
            remainingTokens = tokens;
            while (remainingTokens.Count > 0)
            {
                ParseTree tree;
                ParseLocal(remainingTokens, text, out tree, out remainingTokens);
                trees.Add(tree);
            }
            if (trees.Count == 0)
            {
                throw new ParseException("No tokens in stream.");
            }
            parseTree = new ParseTreeList(tokens[0].StartPosition,
                                          trees.Last.EndPosition,
                                          trees,
                                          SexpParser.GetInstance(ParseType.Global),
                                          null,
                                          text);                                              
        }
        
        public static void ParseLocal(CowList<Token> tokens, 
                                      TextWithChanges text,
                                      out ParseTree parseTree,
                                      out CowList<Token> remainingTokens)
        {
            if (tokens.Count == 0)
            {
                throw new ParseException("No tokens in stream.");
            }
            switch ((TokenTypes)tokens[0].Kind)
            {
            case TokenTypes.Quote:
                ParseTree quotedTree;
                ParseLocal(tokens.Tail,
                           text,
                           out quotedTree,
                           out remainingTokens);
                parseTree = new ParseTreeQuote(tokens[0].StartPosition,
                                               quotedTree.EndPosition,
                                               quotedTree,
                                               SexpParser.GetInstance(ParseType.Sexp),
                                               null,
                                               text);
                return;
            case TokenTypes.OpenParen:
                CowList<ParseTree> members = new CowList<ParseTree>();
                CowList<Token> iterTokens = tokens.Tail;
                while (iterTokens.Count > 0 
                       && (TokenTypes)iterTokens[0].Kind != TokenTypes.CloseParen)
                {
                    ParseTree member;
                    CowList<Token> nextIterTokens;
                    ParseLocal(iterTokens, text, out member, out nextIterTokens);
                    iterTokens = nextIterTokens;
                    members.Add(member);
                }
                if (iterTokens.Count == 0)
                {
                    throw new ParseException("No tokens left in stream, expected a ')'.");
                }
                remainingTokens = iterTokens.Tail;
                parseTree = new ParseTreeList(tokens[0].StartPosition,
                                              iterTokens[0].EndPosition,
                                              members,
                                              SexpParser.GetInstance(ParseType.Sexp),
                                              null,
                                              text);                                              
                return;
            case TokenTypes.CloseParen:
                throw new ParseException("Unexpected ')'.");
            case TokenTypes.Atom:
                remainingTokens = tokens.Tail;
                parseTree = new ParseTreeAtom(tokens[0].StartPosition,
                                              tokens[0].EndPosition,
                                              tokens[0].Content,
                                              SexpParser.GetInstance(ParseType.Sexp),
                                              null,
                                              text);
                return;
            default:
                throw new ParseException(String.Format("Unknown token '{0}'.", 
                                                       tokens[0].ToString()));
            }
        }

        public static CowList<Token> Tokenize(CowList<CharWithPosition> text)
        {
            CowList<Token> result = new CowList<Token>();
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
                        endPos = text[pos].Position + 1;
                        pos ++;
                    }
                    result.Add(new Token((int)TokenTypes.Atom, sb.ToString(), startPos, endPos));
                }
            }
            return result;
        }
        #endregion
    }
}
