
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
            Synpl.Core.Parser parser = SexpParser.Instance;
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

        // TODO: Write tests for ToString() for ParseTreeAtom, ParseTreeList and ParseTreeQuote
        
        // TODO: Write a previous sibling/next sibling test.

        // TODO: Write a moveup/movedown test.

        // TODO: Write an insert char/delete char test.

        #region Private Helper Methods
        // The following functions are debugging helpers, they are not
        // used normally.
        #pragma warning disable 0169
        private CowList<Token> GetTokens(string sourceCode)
        {
            return SexpParser.Instance.TokenizerFunc(EnumerateString(sourceCode));
        }
        
        private ParseTree GetTree(string sourceCode)
        {
            Synpl.Core.Parser parser = SexpParser.Instance;
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
