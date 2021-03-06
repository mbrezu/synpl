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
using NUnit.Framework;
using Synpl.Parser.Sexp;
using System.Collections.Generic;
using Synpl.Core;
using System.Text;

namespace Synpl.Test.Core
{
    
    [TestFixture]
    public class TestSexpParser
    {
        [Test]
        public void TestTokenizer()
        {
            Synpl.Core.Parser parser = SexpParser.GetInstance();
            CowList<CharWithPosition> text = EnumerateString("(+ 1 2)");
            CowList<Token> tokens = parser.TokenizerFunc(text);
            
            CowList<Token> expectedTokens = new CowList<Token>();
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.OpenParen, "(", 0, 1));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "+", 1, 2));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "1", 3, 4));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "2", 5, 6));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.CloseParen, ")", 6, 7));
            
            Assert.AreEqual(expectedTokens, tokens);
        }

        [Test]
        public void TestTokenizerOldVersionExtraChars()
        {
            Synpl.Core.Parser parser = SexpParser.GetInstance();
            TwcBuilder twcb = new TwcBuilder();
            twcb.AddText("(some oth");
            twcb.AppendCharAsChange('(');
            twcb.AddText("er)");
            CowList<CharWithPosition> text = twcb.ToTwc().GetOldSlice(0, twcb.ToTwc().GetActualLength());
            CowList<Token> tokens = parser.TokenizerFunc(text);
            CowList<Token> expectedTokens = new CowList<Token>();
            
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.OpenParen, "(", 0, 1));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "some", 1, 5));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.Atom, "other", 6, 12));
            expectedTokens.Add(new Token((int)SexpParser.TokenTypes.CloseParen, ")", 12, 13));
            
            Assert.AreEqual(expectedTokens, tokens);
        }

        [Test]
        public void TestParserBasic()
        {
            ParseTree parseTree = GetTree("(+ 1 2)");
            string expectedTreeRepr = 
@"(0,7): () (list)
  (1,2): +
  (3,4): 1
  (5,6): 2
";
            Assert.AreEqual(expectedTreeRepr, parseTree.ToStringAsTree());
        }

        [Test]
        public void TestParserAdvanced()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            string expectedTreeRepr =
@"(0,39): () (list)
  (1,4): map
  (5,25): () (list)
    (6,12): lambda
    (13,16): () (list)
      (14,15): x
    (17,24): () (list)
      (18,19): *
      (20,21): x
      (22,23): x
  (26,38): ' (quote)
    (27,38): () (list)
      (28,29): 1
      (30,31): 2
      (32,33): 3
      (34,35): 4
      (36,37): 5
";
            Assert.AreEqual(expectedTreeRepr, parseTree.ToStringAsTree());
        }

        [Test]
        public void TestPathFunctions()
        {
            string sourceCode =
@"
(map (lambda (x) (* x x))
     '(1 2 3))  
";
            ParseTree parseTree = GetTree(sourceCode);
            CowList<Token> tokens = GetTokens(sourceCode);
            int lastAtomPosition = 0;
            foreach (Token token in tokens)
            {
                if ((SexpParser.TokenTypes)token.Kind == SexpParser.TokenTypes.Atom)
                {
                    lastAtomPosition = token.StartPosition;
                }
            }
            CowList<ParseTree> pathToAtom = parseTree.GetPathForPosition(lastAtomPosition);
            Assert.AreEqual(4, pathToAtom.Count);
            ParseTree ptAtom = pathToAtom[pathToAtom.Count - 1];
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            Assert.AreEqual(38, atom.StartPosition);
            Assert.AreEqual(39, atom.EndPosition);
            Assert.AreEqual("3", atom.Content);
            
            CowList<int> path = atom.GetPath();
            Assert.AreEqual(new CowList<int>(2, 0, 2), path);
            
            ParseTree ptAtom2 = parseTree.GetNodeAtPath(path);
            Assert.IsTrue(ptAtom2 is ParseTreeAtom);
            ParseTreeAtom atom2 = ptAtom2 as ParseTreeAtom;
            Assert.AreEqual(atom.StartPosition, atom2.StartPosition);
            Assert.AreEqual(atom.EndPosition, atom2.EndPosition);
            Assert.AreEqual(atom.Content, atom2.Content);
        }

        [Test]
        public void TestGetIndexInParent()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(2, 0, 3));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            Assert.AreEqual(3, atom.GetIndexInParent());
            Assert.AreEqual(-1, parseTree.GetIndexInParent());
        }

        [Test]
        public void TestToString()
        {
            ParseTree parseTree = GetTree("(+ 1 2)");
            Assert.AreEqual("[ParseTree: StartPosition=0, EndPosition=7, SubTrees=3]",
                            parseTree.ToString());
        }

        [Test]
        public void TestPreviousSibling()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(2, 0, 3));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            ParseTree ptPrevAtom = atom.GetPreviousSibling();
            Assert.IsTrue(ptPrevAtom is ParseTreeAtom);
            ParseTreeAtom prevAtom = ptPrevAtom as ParseTreeAtom;
            Assert.AreEqual("3", prevAtom.Content);
            Assert.AreEqual(32, prevAtom.StartPosition);
            Assert.AreEqual(33, prevAtom.EndPosition);
            Assert.AreEqual(new CowList<int>(2, 0, 2), prevAtom.GetPath());
        }
      
        [Test]
        public void TestNextSibling()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3 4 5))");
            ParseTree ptAtom = parseTree.GetNodeAtPath(new CowList<int>(1, 2, 0));
            Assert.IsTrue(ptAtom is ParseTreeAtom);
            ParseTreeAtom atom = ptAtom as ParseTreeAtom;
            ParseTree ptNextAtom = atom.GetNextSibling();
            Assert.IsTrue(ptNextAtom is ParseTreeAtom);
            ParseTreeAtom nextAtom = ptNextAtom as ParseTreeAtom;
            Assert.AreEqual("x", nextAtom.Content);
            Assert.AreEqual(20, nextAtom.StartPosition);
            Assert.AreEqual(21, nextAtom.EndPosition);
            Assert.AreEqual(new CowList<int>(1, 2, 1), nextAtom.GetPath());
        }

        [Test]
        public void TestMoveUp()
        {
            ParseTree parseTree = GetTree(@"
(map (lambda (x) (* x x)) 
     '(1 2 3 4 5))");
            ParseTree quotedList = parseTree.GetNodeAtPath(new CowList<int>(2));
            Assert.IsTrue(quotedList is ParseTreeQuote);
            parseTree = quotedList.MoveUp();
            // The first newline is missing in 'expectedCode' below because
            // it is not part of the (map ...) node in the source above,
            // therefore it will not be rendered when rendering the (map ...)
            // node as text.
            string expectedCode = @"(map '(1 2 3 4 5) 
     (lambda (x) (* x x)))";
            Assert.AreEqual(expectedCode, parseTree.ToStringAsCode(false));
        }
        
        [Test]
        public void TestMoveDown()
        {
            ParseTree parseTree = GetTree(@"
(map (lambda (x) (* x x)) 
     '(1 2 3 4 5))");
            ParseTree mapAtom = parseTree.GetNodeAtPath(new CowList<int>(0));
            parseTree = mapAtom.MoveDown();
            string expectedCode = @"((lambda (x) (* x x)) map 
     '(1 2 3 4 5))";
            Assert.AreEqual(expectedCode, parseTree.ToStringAsCode(false));
        }

        [Test]
        public void TestInsertChar()
        {
            string sample = "()";
            TextWithChanges text = new TextWithChanges();
            text.SetText(sample);
            CowList<Token> tokens = GetTokens(sample);
            CowList<Token> remainingTokens;
            ParseTree parseTree;
            SexpParser.GetInstance().ParserFunc(tokens, text, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            Assert.AreEqual("()", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt('a', 1);
            parseTree = parseTree.CharInsertedAt(' ', 2);
            parseTree = parseTree.CharInsertedAt('b', 3);
            Assert.AreEqual("(a b)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt('\'', 3);
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharInsertedAt(')', 4);
            parseTree = parseTree.CharInsertedAt(')', 5);
            Assert.AreEqual("(a '))b)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_a_ _'A)A)_b_)", text.TestRender());
            parseTree = parseTree.CharInsertedAt('(', 4);
            parseTree = parseTree.CharInsertedAt(' ', 6);
            Assert.AreEqual("(a '() )b)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("(a 'b)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_a_ _'A(A)A A)_b_)", text.TestRender());
            CowList<ParseTree> pathToBTree = parseTree.GetPathForPosition(6);
            ParseTree quotedTree = pathToBTree[1];
            parseTree = quotedTree.MoveUp();
            Assert.AreEqual("('() )b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("('b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_'A(A)A A)_b_ _a_)", text.TestRender());
            parseTree = parseTree.CharInsertedAt(' ', 6);
            parseTree = parseTree.CharInsertedAt('(', 5);
            Assert.AreEqual("('() () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("('() () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("_(_'_(_)_ _(_)_ _b_ _a_)", text.TestRender());
            // Start with this state in the delete char test.
        }

        [Test]
        public void TestDeleteChar()
        {
            string sample = "('() () b a)";
            TextWithChanges text = new TextWithChanges();
            text.SetText(sample);
            CowList<Token> tokens = GetTokens(sample);
            CowList<Token> remainingTokens;
            ParseTree parseTree;
            SexpParser.GetInstance().ParserFunc(tokens, text, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            parseTree = parseTree.CharDeletedAt(1);
            Assert.AreEqual("(() () b a)", parseTree.ToStringAsCode(false));
            parseTree = parseTree.CharDeletedAt(2);
            Assert.AreEqual("(() () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("(( () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("_(_(D)_ _(_)_ _b_ _a_)", text.TestRender());
            parseTree = parseTree.CharDeletedAt(1);
            Assert.AreEqual("( () b a)", parseTree.ToStringAsCode(true));
            Assert.AreEqual("( () b a)", parseTree.ToStringAsCode(false));
            Assert.AreEqual("_(_ _(_)_ _b_ _a_)", text.TestRender());
        }

        [Test]
        public void TestHasEndPosition()
        {
            ParseTree parseTree = GetTree(@"
(map (lambda (x) (* x x)) 
     '(1 2 3 4 5))");
            ParseTree lambdaAtom = parseTree.HasEndPosition(13);
            Assert.IsTrue(lambdaAtom is ParseTreeAtom);
            Assert.AreEqual(7, lambdaAtom.StartPosition);
            Assert.AreEqual(13, lambdaAtom.EndPosition);
            Assert.AreEqual("lambda", ((ParseTreeAtom)lambdaAtom).Content);
            Assert.AreEqual(null, parseTree.HasEndPosition(14));
        }

        [Test]
        public void TestAllNodes()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3))");
            CowList<string> expectedCode = 
                new CowList<string>("(map (lambda (x) (* x x)) '(1 2 3))",
                                    "map",
                                    "(lambda (x) (* x x))",
                                    "lambda",
                                    "(x)",
                                    "x",
                                    "(* x x)",
                                    "*",
                                    "x",
                                    "x",
                                    "'(1 2 3)",
                                    "(1 2 3)",
                                    "1",
                                    "2",
                                    "3");
            int i = 0;
            foreach (ParseTree pt in parseTree.AllNodes())
            {
                Assert.AreEqual(expectedCode[i], pt.ToStringAsCode(true));
                i++;
            }
        }

        [Test]
        public void TestGetFirstNodeAfter()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3))");
            ParseTreeQuote qlist = parseTree.GetNodeAtPath(new CowList<int>(2)) as ParseTreeQuote;
            Assert.IsTrue(qlist != null);
            int edge = qlist.StartPosition - 1;
            Assert.AreEqual(qlist, parseTree.GetFirstNodeAfter(edge));
            Assert.AreEqual(null, parseTree.GetFirstNodeAfter(100));
        }
        
        [Test]
        public void TestGetLastNodeBefore()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3))");
            ParseTreeQuote qlist = parseTree.GetNodeAtPath(new CowList<int>(2)) as ParseTreeQuote;
            Assert.IsTrue(qlist != null);
            int edge = qlist.StartPosition - 1;
            ParseTree lastX = parseTree.GetLastNodeBefore(edge);
            ParseTree expectedLastX = parseTree.GetNodeAtPath(new CowList<int>(1, 2, 2));
            Assert.AreEqual(expectedLastX, lastX);
            Assert.AreEqual(null, parseTree.GetLastNodeBefore(0));            
        }
        
        [Test]
        public void TestGetLastNodeBefore2()
        {
            ParseTree parseTree = GetTree("'(1 2 3)");
            ParseTreeAtom atom2 = parseTree.GetNodeAtPath(new CowList<int>(0, 1)) as ParseTreeAtom;
            Assert.IsTrue(atom2 != null);
            int edge = atom2.StartPosition - 1;
            ParseTree atom1 = parseTree.GetLastNodeBefore(edge);
            ParseTree expectedAtom1 = parseTree.GetNodeAtPath(new CowList<int>(0, 0));
            Assert.AreEqual(expectedAtom1, atom1);
        }
        
        [Test]
        public void TestGetLastSiblingBefore()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3))");
            ParseTreeQuote qlist = parseTree.GetNodeAtPath(new CowList<int>(2)) as ParseTreeQuote;
            Assert.IsTrue(qlist != null);
            int edge = qlist.StartPosition - 1;
            ParseTree lambda = qlist.GetLastSiblingBefore(edge);
            ParseTree expectedLambda = parseTree.GetNodeAtPath(new CowList<int>(1));
            Assert.AreEqual(expectedLambda, lambda);
            Assert.AreEqual(null, parseTree.GetLastSiblingBefore(0));            
        }
        
        [Test]
        public void TestGetFirstSiblingAfter()
        {
            ParseTree parseTree = GetTree("(map (lambda (x) (* x x)) '(1 2 3))");
            ParseTreeAtom lambda = parseTree.GetNodeAtPath(new CowList<int>(1, 0)) as ParseTreeAtom;
            Assert.IsTrue(lambda != null);
            int edge = lambda.EndPosition + 1;
            ParseTree argList = lambda.GetFirstSiblingAfter(edge);
            ParseTree expectedArgList = parseTree.GetNodeAtPath(new CowList<int>(1, 1));
            Assert.AreEqual(expectedArgList, argList);
            Assert.AreEqual(null, parseTree.GetFirstNodeAfter(100));            
        }
        
        #region Private Helper Methods
        // The following functions are debugging helpers, they are not
        // used normally.
        #pragma warning disable 0169
        private CowList<Token> GetTokens(string sourceCode)
        {
            return SexpParser.GetInstance().TokenizerFunc(EnumerateString(sourceCode));
        }
        
        private ParseTree GetTree(string sourceCode)
        {
            Synpl.Core.Parser parser = SexpParser.GetInstance();
            CowList<CharWithPosition> text = EnumerateString(sourceCode);
            CowList<Token> tokens = parser.TokenizerFunc(text);
            TextWithChanges textWithChanges = new TextWithChanges();
            textWithChanges.SetText(sourceCode);
            ParseTree parseTree;
            CowList<Token> remainingTokens;
            parser.ParserFunc(tokens, textWithChanges, out parseTree, out remainingTokens);
            Assert.AreEqual(0, remainingTokens.Count);
            return parseTree;
        }
        
        private string DumpCollection<T>(IList<T> collection)
        {
            StringBuilder sb = new StringBuilder();
            foreach (T item in collection)
            {
                sb.Append(item.ToString());
                sb.Append("\n");
            }
            return sb.ToString();
        }
        
        private CowList<CharWithPosition> EnumerateString(string str)
        {
            CowList<CharWithPosition> result = new CowList<CharWithPosition>();
            for (int i = 0; i < str.Length; i++)
            {
                result.Add(new CharWithPosition(i, str[i]));
            }
            return result;
        }        
        #endregion
    }
}
