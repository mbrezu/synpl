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
using System.Collections.Generic;

namespace Synpl.Core
{  
    public class Parser
    {
        #region Delegates
        public delegate CowList<Token> TokenizerFunction(CowList<CharWithPosition> text);                                          
        public delegate void ParserFunction(CowList<Token> tokens, 
                                            TextWithChanges textWithChanges,
                                            out ParseTree parseTree,
                                            out CowList<Token> remainingTokens);
        #endregion

        #region Private Storage
        private TokenizerFunction _tokenizerFunc;
        private ParserFunction _parserFunc;
        #endregion

        #region Properties
        public ParserFunction ParserFunc {
            get {
                return _parserFunc;
            }
            set
            {
                _parserFunc = value;
            }
        }

        public TokenizerFunction TokenizerFunc {
            get {
                return _tokenizerFunc;
            }
        }
        #endregion

        #region Constructor
        public Parser(ParserFunction parserFunc, 
                      TokenizerFunction tokenizerFunc)
        {
            _parserFunc = parserFunc;
            _tokenizerFunc = tokenizerFunc;
        }
        #endregion

        #region Public Methods
        #endregion
    }
}
